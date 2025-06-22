"""
GridCanvas - Core drawing canvas component for the MapDrawer
Handles all grid rendering, drawing modes, mouse/keyboard interaction, and zoom functionality
"""

import tkinter as tk
import math


class GridCanvas(tk.Canvas):
    def __init__(self, parent, map_drawer=None, **kwargs):
        super().__init__(parent, **kwargs)
        self.parent_window = parent
        self.map_drawer = map_drawer
        
        # Grid settings (must be even numbers)
        self.cell_size = 20
        self.base_cell_size = 20  # Base cell size for zoom calculations
        self.zoom_level = 1.0  # Current zoom level (1.0 = 100%)
        self.zoom_levels = [0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0]  # Discrete zoom levels like Aseprite
        self.zoom_index = 3  # Start at 100% (index 3 in zoom_levels)
        self.grid_width = 40  # Must be even
        self.grid_height = 30  # Must be even
        
        # Drawing settings
        self.drawing_mode = 'wall'
        self.is_drawing = False
        self.last_pos = None
        self.show_guide_grid = True
        
        # Axis snapping for wall drawing
        self.drawing_axis = None  # 'horizontal', 'vertical', or None
        self.axis_start_pos = None  # Starting position for axis locking
        
        # Grid data
        self.grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
        self.spike_grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
        
        # Asterisk path tracking
        self.asterisk_path = []  # List of (x, y) coordinates for the asterisk path
        self.asterisk_directions = {}  # Dictionary mapping (x,y) -> list of directions
        self.asterisk_direction_sequence = []  # Ordered list of directions taken
        self.asterisk_cursor = None  # Current cursor position (x, y) when drawing asterisk path
        self.asterisk_drawing_active = False  # Whether user is actively drawing asterisk path
        self.asterisk_last_direction = None  # Track last direction for consecutive move detection
        
        # Path marking for Start and Finish entry/exit points
        self.start_path_marks = []  # List of directions marked for start entry (e.g., ['top', 'left'])
        self.finish_path_marks = []  # List of directions marked for finish exit (e.g., ['bottom', 'right'])
        
        # Selection for copy/paste operations
        self.selection_start = None
        self.selection_end = None
        self.selection_active = False
        self.clipboard = None  # Store copied selection data
        
        # History for undo/redo functionality
        self.history = []
        self.history_index = -1
        self.max_history = 50  # Maximum number of undo steps
        
        # Save initial state
        self.save_state()
        
        # Bind mouse events
        self.bind("<Button-1>", self.on_mouse_press)
        self.bind("<B1-Motion>", self.on_mouse_drag)
        self.bind("<ButtonRelease-1>", self.on_mouse_release)
        
        # Bind keyboard events for asterisk drawing
        self.bind("<Key>", self.on_key_press)
        self.focus_set()  # Make canvas focusable
        
        self.update_canvas()
        
    def set_grid_size(self, width, height):
        # Ensure grid sizes are even (multiples of 2)
        new_width = width if width % 2 == 0 else width + 1
        new_height = height if height % 2 == 0 else height + 1
        
        # Store current grid content
        old_grid = self.grid
        old_width = self.grid_width
        old_height = self.grid_height
        
        # Update dimensions
        self.grid_width = new_width
        self.grid_height = new_height
        
        # Create new grid with proper size
        self.grid = [['.' for _ in range(new_width)] for _ in range(new_height)]
        
        # Copy existing content (preserve top-left, remove from right/bottom if shrinking)
        copy_width = min(old_width, new_width)
        copy_height = min(old_height, new_height)
        
        for y in range(copy_height):
            for x in range(copy_width):
                self.grid[y][x] = old_grid[y][x]
        
        # Preserve asterisk path state and other states that reference grid positions
        # Filter out asterisk positions that are now outside the grid
        if hasattr(self, 'asterisk_path'):
            self.asterisk_path = [(x, y) for x, y in self.asterisk_path 
                                 if x < new_width and y < new_height]
        
        if hasattr(self, 'asterisk_directions'):
            # Remove directions for positions outside the new grid
            valid_directions = {}
            for pos, directions in self.asterisk_directions.items():
                x, y = pos
                if x < new_width and y < new_height:
                    valid_directions[pos] = directions
            self.asterisk_directions = valid_directions
        
        # Update asterisk cursor if it's outside the new bounds
        if hasattr(self, 'asterisk_cursor') and self.asterisk_cursor:
            cursor_x, cursor_y = self.asterisk_cursor
            if cursor_x + 1 >= new_width or cursor_y + 1 >= new_height:
                self.asterisk_cursor = None
                self.asterisk_drawing_active = False
        
        # Save state and update canvas
        self.save_state()
        self.update_canvas()
        
    def set_drawing_mode(self, mode):
        self.drawing_mode = mode
        
    def toggle_guide_grid(self, show):
        self.show_guide_grid = show
        self.update_canvas()
        
    def clear_grid(self):
        self.grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
        self.spike_grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
        self.save_state()
        self.update_canvas()
        
    def get_grid_pos(self, canvas_x, canvas_y):
        """Convert canvas coordinates to grid coordinates"""
        if canvas_x < 0 or canvas_y < 0:
            return None, None
        
        grid_x = int(canvas_x // self.cell_size)
        grid_y = int(canvas_y // self.cell_size)
        
        if grid_x >= self.grid_width or grid_y >= self.grid_height:
            return None, None
            
        return grid_x, grid_y
        
    def place_2x2_block(self, grid_x, grid_y, symbol):
        """Place a 2x2 block (for S, F, or *) - only one of each type allowed"""
        # Align to even coordinates for 2x2 placement
        align_x = (grid_x // 2) * 2
        align_y = (grid_y // 2) * 2
        
        # Check bounds
        if align_x + 1 >= self.grid_width or align_y + 1 >= self.grid_height:
            return False
        
        # Special validation for asterisk (*) - can only be placed in yellow areas
        if symbol == '*':
            if not self.can_place_asterisk(align_x, align_y):
                return False
        
        # Remove existing blocks of the same type (only one start/finish allowed)
        if symbol in ['S', 'F']:
            self.remove_existing_block(symbol)
            
        # Place 2x2 block
        for dy in range(2):
            for dx in range(2):
                self.grid[align_y + dy][align_x + dx] = symbol
        
        # If placing Start, trigger flood fill to mark accessible areas
        if symbol == 'S':
            self.flood_fill_from_start()
            self.update_wall_types_from_flood()
                
        return True
    
    def start_asterisk_drawing(self):
        """
        Start manual asterisk path drawing from Start block
        User will use arrow keys to draw the path in 2x2 blocks
        """
        # Clear existing asterisk path
        self.clear_asterisk_path()
        
        # Find Start block
        start_pos = self.find_block_position('S')
        if not start_pos:
            if self.map_drawer:
                self.map_drawer.status_var.set("No Start block found! Place a Start block first.")
            return False
        
        # Set cursor to top-left of Start block (for 2x2 alignment)
        start_x, start_y = start_pos
        self.asterisk_cursor = (start_x, start_y)  # Top-left of 2x2 block
        self.asterisk_drawing_active = True
        
        # Initialize path and directions
        self.asterisk_path = [self.asterisk_cursor]
        
        # For direction tracking, use the top-left position of the 2x2 block
        top_left_pos = (start_x, start_y)
        self.asterisk_directions = {top_left_pos: []}
        self.asterisk_direction_sequence = []
        self.asterisk_last_direction = None  # Reset direction tracking
        
        # Mark starting 2x2 position (but don't overwrite Start block)
        for dy in range(2):
            for dx in range(2):
                mark_x = start_x + dx
                mark_y = start_y + dy
                if (mark_x < self.grid_width and mark_y < self.grid_height and 
                    self.grid[mark_y][mark_x] not in ['S', 'F']):
                    self.grid[mark_y][mark_x] = '*'
        
        # Save initial state when starting asterisk drawing
        self.save_state()
        if self.map_drawer:
            self.map_drawer.update_undo_redo_buttons()
        
        self.update_canvas()
        
        if self.map_drawer:
            self.map_drawer.status_var.set("Asterisk drawing mode active! Use arrow keys to draw 2x2 path blocks. ESC to exit.")
        
        # Focus the canvas so it can receive key events
        self.focus_set()
        return True
    
    def move_asterisk_cursor(self, direction):
        """
        Draw a continuous line of 2x2 asterisk blocks in the specified direction until hitting a wall
        direction: 'up', 'down', 'left', 'right'
        """
        if not self.asterisk_drawing_active or not self.asterisk_cursor:
            return
        
        direction_vectors = {
            'up': (0, -2),      # Move by 2 cells for 2x2 blocks
            'down': (0, 2), 
            'left': (-2, 0),
            'right': (2, 0)
        }
        
        direction_symbols = {
            'up': '↑',
            'down': '↓', 
            'left': '←',
            'right': '→'
        }
        
        if direction not in direction_vectors:
            return
        
        # Check if this is a consecutive move in the same direction
        is_consecutive = (self.asterisk_last_direction == direction)
        direction_changed = not is_consecutive and self.asterisk_last_direction is not None
        
        # Starting position
        cursor_x, cursor_y = self.asterisk_cursor
        dx, dy = direction_vectors[direction]
        
        blocks_drawn = 0
        
        # Save state when direction changes (before drawing new blocks)
        if direction_changed:
            self.save_state()
            if self.map_drawer:
                self.map_drawer.update_undo_redo_buttons()
        
        # Keep drawing in the direction until we hit a wall or boundary
        while True:
            # Calculate next position
            next_x = cursor_x + dx
            next_y = cursor_y + dy
            
            # Check bounds for 2x2 block
            if (next_x < 0 or next_x + 1 >= self.grid_width or 
                next_y < 0 or next_y + 1 >= self.grid_height):
                break
            
            # Check if any part of the 2x2 block would hit a wall
            wall_hit = False
            finish_reached = False
            
            for dy_check in range(2):
                for dx_check in range(2):
                    check_x = next_x + dx_check
                    check_y = next_y + dy_check
                    check_cell = self.grid[check_y][check_x]
                    
                    # Only walls block movement, not existing asterisks
                    if check_cell in ['#', '1', '2', '3', '4', '5', '6', '7', '8', '9']:
                        wall_hit = True
                        break
                    elif check_cell == 'F':
                        finish_reached = True
                
                if wall_hit:
                    break
            
            if wall_hit:
                break
            
            # Move cursor to new position
            cursor_x, cursor_y = next_x, next_y
            
            # Record the direction at the TOP-LEFT position of this 2x2 block
            # This makes it easier to display the arrow later
            block_top_left = (cursor_x, cursor_y)
            if block_top_left in self.asterisk_directions:
                self.asterisk_directions[block_top_left].append(direction)
            else:
                self.asterisk_directions[block_top_left] = [direction]
            
            # Add to direction sequence
            self.asterisk_direction_sequence.append(direction)
            
            # Add to path if not already there
            cursor_pos = (cursor_x, cursor_y)
            if cursor_pos not in self.asterisk_path:
                self.asterisk_path.append(cursor_pos)
            
            # Place the 2x2 asterisk block (unless it overlaps with Start or Finish)
            for dy_place in range(2):
                for dx_place in range(2):
                    place_x = cursor_x + dx_place
                    place_y = cursor_y + dy_place
                    current_cell = self.grid[place_y][place_x]
                    
                    # Don't overwrite Start or Finish blocks
                    if current_cell not in ['S', 'F']:
                        self.grid[place_y][place_x] = '*'
            
            blocks_drawn += 1
            
            # Check if we reached Finish block
            if finish_reached:
                # Update cursor position and finish
                self.asterisk_cursor = (cursor_x, cursor_y)
                # Save state for this complete move (including finish)
                if blocks_drawn > 0:
                    self.save_state()
                    if self.map_drawer:
                        self.map_drawer.update_undo_redo_buttons()
                self.finish_asterisk_drawing()
                return
        
        # Update cursor to final position
        self.asterisk_cursor = (cursor_x, cursor_y)
        
        # Update last direction tracker
        if blocks_drawn > 0:
            self.asterisk_last_direction = direction
        
        # Save state at the end of the move sequence (for the last move or single moves)
        if blocks_drawn > 0:
            self.save_state()
            if self.map_drawer:
                self.map_drawer.update_undo_redo_buttons()
        
        self.update_canvas()
        
        if self.map_drawer:
            if blocks_drawn > 0:
                # Show compressed direction count
                compressed_directions = self.compress_direction_sequence(self.asterisk_direction_sequence)
                total_compressed = len(compressed_directions)
                consecutive_info = " (consecutive)" if is_consecutive else ""
                self.map_drawer.status_var.set(f"Drew {blocks_drawn} blocks {direction_symbols[direction]}{consecutive_info}. Compressed moves: {total_compressed}. Use arrow keys to continue, ESC to finish.")
            else:
                self.map_drawer.status_var.set(f"Cannot move {direction} - blocked by wall. Use other arrow keys or ESC to finish.")
    
    def finish_asterisk_drawing(self):
        """
        Finish asterisk drawing mode
        """
        self.asterisk_drawing_active = False
        self.asterisk_cursor = None
        
        if self.map_drawer:
            compressed_directions = self.compress_direction_sequence(self.asterisk_direction_sequence)
            compressed_count = len(compressed_directions)
            self.map_drawer.status_var.set(f"Asterisk path completed! Compressed to {compressed_count} moves.")
            
            # Exit asterisk mode and return to wall mode
            self.map_drawer.set_drawing_mode('wall')
        
        self.update_canvas()
    
    def clear_asterisk_path(self):
        """Clear all asterisk blocks from the grid"""
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                if self.grid[y][x] == '*':
                    self.grid[y][x] = '.'
        
        self.asterisk_path = []
        self.asterisk_directions = {}
        self.asterisk_direction_sequence = []
        self.asterisk_cursor = None
        self.asterisk_drawing_active = False
        self.asterisk_last_direction = None
    
    def find_block_position(self, symbol):
        """Find the top-left position of a 2x2 block (S or F)"""
        for y in range(self.grid_height - 1):
            for x in range(self.grid_width - 1):
                if (self.grid[y][x] == symbol and 
                    self.grid[y][x+1] == symbol and
                    self.grid[y+1][x] == symbol and
                    self.grid[y+1][x+1] == symbol):
                    return (x, y)
        return None
    
    def remove_existing_block(self, symbol):
        """Remove all existing blocks of the given symbol type"""
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                if self.grid[y][x] == symbol:
                    self.grid[y][x] = '.'
    
    def mark_path_direction(self, grid_x, grid_y, path_type):
        """
        Mark a direction for Start or Finish path entry/exit
        path_type: 'start_path' or 'finish_path'
        """
        # Find the appropriate block (Start or Finish)
        target_symbol = 'S' if path_type == 'start_path' else 'F'
        block_pos = self.find_block_position(target_symbol)
        
        if not block_pos:
            if self.map_drawer:
                block_name = "Start" if path_type == 'start_path' else "Finish"
                self.map_drawer.status_var.set(f"No {block_name} block found! Place a {block_name} block first.")
            return
        
        block_x, block_y = block_pos
        
        # Determine which direction was clicked relative to the 2x2 block
        # The block occupies (block_x, block_y) to (block_x+1, block_y+1)
        direction = None
        
        # Check if click is adjacent to the 2x2 block
        if grid_y == block_y - 1 and block_x <= grid_x <= block_x + 1:
            direction = 'top'
        elif grid_y == block_y + 2 and block_x <= grid_x <= block_x + 1:
            direction = 'bottom'
        elif grid_x == block_x - 1 and block_y <= grid_y <= block_y + 1:
            direction = 'left'
        elif grid_x == block_x + 2 and block_y <= grid_y <= block_y + 1:
            direction = 'right'
        
        if not direction:
            if self.map_drawer:
                self.map_drawer.status_var.set("Click adjacent to the Start/Finish block to mark entry/exit direction.")
            return
        
        # Get the appropriate marks list
        marks_list = self.start_path_marks if path_type == 'start_path' else self.finish_path_marks
        
        # Toggle the direction (add if not present, remove if present)
        if direction in marks_list:
            marks_list.remove(direction)
            action = "removed"
        else:
            marks_list.append(direction)
            action = "added"
        
        # Update display and save state
        self.save_state()
        self.update_canvas()
        
        if self.map_drawer:
            block_name = "Start" if path_type == 'start_path' else "Finish"
            marks_count = len(marks_list)
            self.map_drawer.status_var.set(f"{block_name} path mark {action}: {direction} (total: {marks_count})")
            self.map_drawer.update_undo_redo_buttons()
            print(f"DEBUG: {block_name} path marks: {marks_list}")  # Debug output
    
    def get_possible_entry_exit_directions(self):
        """
        Convert marked directions to the opposite directions for entry/exit
        Returns (start_entry_directions, finish_exit_directions)
        """
        # Convert marked directions to entry/exit directions (opposite)
        direction_opposites = {
            'top': 'down',
            'bottom': 'up', 
            'left': 'right',
            'right': 'left'
        }
        
        start_entry = [direction_opposites[d] for d in self.start_path_marks]
        finish_exit = [direction_opposites[d] for d in self.finish_path_marks]
        
        return start_entry, finish_exit
        
    def erase_2x2_block_if_needed(self, grid_x, grid_y):
        """
        Check if the clicked position is part of a 2x2 block (S, F, or *) and erase the entire block
        Uses 2x2 grid snapping just like placement logic
        Returns True if a 2x2 block was erased, False otherwise
        """
        cell_value = self.grid[grid_y][grid_x]
        
        # Only handle 2x2 block types
        if cell_value not in ['S', 'F', '*']:
            return False
        
        # Snap to 2x2 grid boundaries (same logic as placement)
        align_x = (grid_x // 2) * 2
        align_y = (grid_y // 2) * 2
        
        # Check bounds for the aligned 2x2 block
        if align_x + 1 >= self.grid_width or align_y + 1 >= self.grid_height:
            return False
        
        # Verify that all 4 cells in the aligned 2x2 block contain the same symbol
        target_symbol = self.grid[align_y][align_x]
        if target_symbol not in ['S', 'F', '*']:
            return False
            
        # Check if it's a valid 2x2 block
        for dy in range(2):
            for dx in range(2):
                check_x, check_y = align_x + dx, align_y + dy
                if (check_x >= self.grid_width or check_y >= self.grid_height or 
                    self.grid[check_y][check_x] != target_symbol):
                    return False
        
        # Erase the entire aligned 2x2 block
        for dy in range(2):
            for dx in range(2):
                erase_x, erase_y = align_x + dx, align_y + dy
                self.grid[erase_y][erase_x] = '.'
        
        # Update surrounding walls and flood fill if Start was removed
        if target_symbol == 'S':
            self.flood_fill_from_start()
            self.update_wall_types_from_flood()
        else:
            # Update surrounding area for wall orientation
            self.update_wall_types_in_area(align_x - 1, align_y - 1, 
                                         align_x + 2, align_y + 2)
        
        return True
    
    def find_2x2_block_origin(self, click_x, click_y, symbol):
        """
        Find the top-left corner of a 2x2 block given any cell within it
        Returns (top_left_x, top_left_y) or (None, None) if not a valid 2x2 block
        """
        # Check all possible top-left positions for a 2x2 block containing the clicked cell
        for offset_y in range(2):
            for offset_x in range(2):
                potential_top_x = click_x - offset_x
                potential_top_y = click_y - offset_y
                
                # Check if this could be a valid 2x2 block origin
                if (potential_top_x >= 0 and potential_top_y >= 0 and 
                    potential_top_x + 1 < self.grid_width and potential_top_y + 1 < self.grid_height):
                    
                    # Check if all 4 cells contain the same symbol
                    is_valid_block = True
                    for dy in range(2):
                        for dx in range(2):
                            check_x, check_y = potential_top_x + dx, potential_top_y + dy
                            if self.grid[check_y][check_x] != symbol:
                                is_valid_block = False
                                break
                        if not is_valid_block:
                            break
                    
                    if is_valid_block:
                        return potential_top_x, potential_top_y
        
        return None, None

    def get_wall_type(self, x, y):
        """
        Determine the wall type based on flood-filled accessible areas (yellow marks)
        Walls face towards the nearest accessible (yellow) areas
        Returns a number 1-9 representing different wall orientations
        """
        if not self.is_wall(x, y):
            return '.'
            
        # Check for yellow marks (accessible areas) in all 8 directions
        directions = {
            'top_left': (x-1, y-1),
            'top': (x, y-1), 
            'top_right': (x+1, y-1),
            'left': (x-1, y),
            'right': (x+1, y),
            'bottom_left': (x-1, y+1),
            'bottom': (x, y+1),
            'bottom_right': (x+1, y+1)
        }
        
        # Check which directions have yellow marks (accessible areas)
        yellow_dirs = []
        for direction, (check_x, check_y) in directions.items():
            if self.has_yellow_mark(check_x, check_y):
                yellow_dirs.append(direction)
        
        # Also check for neighboring walls to determine corner/edge type
        wall_neighbors = {
            'left': self.is_wall(x - 1, y),
            'right': self.is_wall(x + 1, y),
            'top': self.is_wall(x, y - 1),
            'bottom': self.is_wall(x, y + 1)
        }
        
        # Special corner detection based on 2x2 chunk position
        # Check if this wall is at a specific position within a 2x2 chunk
        chunk_x = x % 2
        chunk_y = y % 2
        
        # Corner case: wall at bottom-right of 2x2 chunk (chunk_x=1, chunk_y=1)
        if chunk_x == 1 and chunk_y == 1:
            # Check if bottom-right diagonal has a wall, and bottom+right have commas
            if (self.is_wall(x + 1, y + 1) and 
                self.has_yellow_mark(x, y + 1) and 
                self.has_yellow_mark(x + 1, y)):
                return '3'  # Bottom-right corner
        
        # Corner case: wall at bottom-left of 2x2 chunk (chunk_x=0, chunk_y=1)
        if chunk_x == 0 and chunk_y == 1:
            # Check if bottom-left diagonal has a wall, and bottom+left have commas
            if (self.is_wall(x - 1, y + 1) and 
                self.has_yellow_mark(x, y + 1) and 
                self.has_yellow_mark(x - 1, y)):
                return '1'  # Bottom-left corner
        
        # Corner case: wall at top-right of 2x2 chunk (chunk_x=1, chunk_y=0)
        if chunk_x == 1 and chunk_y == 0:
            # Check if top-right diagonal has a wall, and top+right have commas
            if (self.is_wall(x + 1, y - 1) and 
                self.has_yellow_mark(x, y - 1) and 
                self.has_yellow_mark(x + 1, y)):
                return '9'  # Top-right corner
        
        # Corner case: wall at top-left of 2x2 chunk (chunk_x=0, chunk_y=0)
        if chunk_x == 0 and chunk_y == 0:
            # Check if top-left diagonal has a wall, and top+left have commas
            if (self.is_wall(x - 1, y - 1) and 
                self.has_yellow_mark(x, y - 1) and 
                self.has_yellow_mark(x - 1, y)):
                return '7'  # Top-left corner

        # Determine wall orientation based on accessible areas and wall neighbors
        # Corner cases - check for accessible areas in diagonal directions
        if 'bottom_right' in yellow_dirs or ('bottom' in yellow_dirs and 'right' in yellow_dirs):
            if wall_neighbors['right'] and wall_neighbors['bottom']:
                return '7'  # Top-left corner (was 1, now flipped)
        
        if 'bottom_left' in yellow_dirs or ('bottom' in yellow_dirs and 'left' in yellow_dirs):
            if wall_neighbors['left'] and wall_neighbors['bottom']:
                return '9'  # Top-right corner (was 3, now flipped)
                
        if 'top_right' in yellow_dirs or ('top' in yellow_dirs and 'right' in yellow_dirs):
            if wall_neighbors['right'] and wall_neighbors['top']:
                return '1'  # Bottom-left corner (was 7, now flipped)
                
        if 'top_left' in yellow_dirs or ('top' in yellow_dirs and 'left' in yellow_dirs):
            if wall_neighbors['left'] and wall_neighbors['top']:
                return '3'  # Bottom-right corner (was 9, now flipped)
        
        # Additional corner detection - check for multiple yellow directions
        # Bottom-right corner: yellow to right, bottom-right, and bottom + walls on top and left
        if ('right' in yellow_dirs and 'bottom_right' in yellow_dirs and 'bottom' in yellow_dirs):
            if wall_neighbors['top'] and wall_neighbors['left']:
                return '3'  # Bottom-right corner
                
        # Bottom-left corner: yellow to left, bottom-left, and bottom + walls on top and right
        if ('left' in yellow_dirs and 'bottom_left' in yellow_dirs and 'bottom' in yellow_dirs):
            if wall_neighbors['top'] and wall_neighbors['right']:
                return '1'  # Bottom-left corner
                
        # Top-right corner: yellow to right, top-right, and top + walls on bottom and left
        if ('right' in yellow_dirs and 'top_right' in yellow_dirs and 'top' in yellow_dirs):
            if wall_neighbors['bottom'] and wall_neighbors['left']:
                return '9'  # Top-right corner
                
        # Top-left corner: yellow to left, top-left, and top + walls on bottom and right
        if ('left' in yellow_dirs and 'top_left' in yellow_dirs and 'top' in yellow_dirs):
            if wall_neighbors['bottom'] and wall_neighbors['right']:
                return '7'  # Top-left corner
        
        # Edge cases - check for accessible areas in cardinal directions
        if any(dir in yellow_dirs for dir in ['bottom', 'bottom_left', 'bottom_right']):
            if wall_neighbors['left'] and wall_neighbors['right'] and not wall_neighbors['bottom']:
                return '8'  # Top edge (was 2, now flipped)
                
        if any(dir in yellow_dirs for dir in ['top', 'top_left', 'top_right']):
            if wall_neighbors['left'] and wall_neighbors['right'] and not wall_neighbors['top']:
                return '2'  # Bottom edge (was 8, now flipped)
                
        if any(dir in yellow_dirs for dir in ['left', 'top_left', 'bottom_left']):
            if wall_neighbors['top'] and wall_neighbors['bottom'] and not wall_neighbors['left']:
                return '6'  # Right edge (was 4, now flipped)
                
        if any(dir in yellow_dirs for dir in ['right', 'top_right', 'bottom_right']):
            if wall_neighbors['top'] and wall_neighbors['bottom'] and not wall_neighbors['right']:
                return '4'  # Left edge (was 6, now flipped)
        
        # Default to center if no clear pattern
        return '5'  # Center or standalone wall
    
    def has_yellow_mark(self, x, y):
        """Check if position has a yellow mark (accessible area) - includes Start, Finish, and Asterisk"""
        if x < 0 or x >= self.grid_width or y < 0 or y >= self.grid_height:
            return False
        cell = self.grid[y][x]
        return cell in [',', 'S', 'F', '*']  # Include Start, Finish, and Asterisk as accessible areas
    
    def is_wall(self, x, y):
        """Check if position contains any wall type"""
        if x < 0 or x >= self.grid_width or y < 0 or y >= self.grid_height:
            return False
        cell = self.grid[y][x]
        return cell in ['#', '1', '2', '3', '4', '5', '6', '7', '8', '9']
    
    def flood_fill_from_start(self):
        """Flood fill accessible areas from Start position with yellow marks (commas)"""
        # Clear existing yellow marks
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                if self.grid[y][x] == ',':
                    self.grid[y][x] = '.'
        
        # Find start position
        start_positions = []
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                if self.grid[y][x] == 'S':
                    start_positions.append((x, y))
        
        if not start_positions:
            return  # No start position found
        
        # Flood fill from all start positions
        visited = set()
        queue = []
        
        # Add all start positions to queue
        for start_x, start_y in start_positions:
            queue.append((start_x, start_y))
            visited.add((start_x, start_y))
        
        # Flood fill algorithm
        while queue:
            x, y = queue.pop(0)
            
            # Check if this position can be marked as accessible
            if self.can_mark_as_accessible(x, y):
                # Mark as accessible if it's empty space (don't overwrite S or F)
                if self.grid[y][x] == '.':
                    self.grid[y][x] = ','
                
                # Check all 4 directions (not diagonal for flood fill)
                for dx, dy in [(0, 1), (0, -1), (1, 0), (-1, 0)]:
                    nx, ny = x + dx, y + dy
                    if (nx, ny) not in visited and 0 <= nx < self.grid_width and 0 <= ny < self.grid_height:
                        if not self.is_wall(nx, ny):  # Can reach any non-wall including S and F
                            visited.add((nx, ny))
                            queue.append((nx, ny))
    
    def can_mark_as_accessible(self, x, y):
        """Check if position can be marked as accessible (part of 2x2 chunk)"""
        # Don't overwrite Start, Finish, and Asterisk blocks, but allow them to be treated as accessible
        if self.grid[y][x] in ['S', 'F', '*']:
            return True  # Already accessible, don't change
        # For other cells, check if they can be marked
        return not self.is_wall(x, y) and self.grid[y][x] not in ['S', 'F', '*']
    
    def update_wall_types_from_flood(self):
        """Update all wall types based on flood-filled accessible areas"""
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                if self.is_wall(x, y):
                    new_type = self.get_wall_type(x, y)
                    if new_type != '.':
                        self.grid[y][x] = new_type
    
    def update_wall_types_in_area(self, start_x, start_y, end_x, end_y):
        """Update wall types in a rectangular area using flood-fill method"""
        # When walls change, we need to re-flood fill and update wall orientations
        self.flood_fill_from_start()
        self.update_wall_types_from_flood()

    def draw_wall_line(self, start_x, start_y, end_x, end_y):
        """Draw a line of walls between two points with smart wall typing"""
        if start_x == end_x:  # Vertical line
            y1, y2 = min(start_y, end_y), max(start_y, end_y)
            for y in range(y1, y2 + 1):
                if 0 <= y < self.grid_height:
                    self.grid[y][start_x] = '#'  # Place basic wall first
            # Update wall types for the entire line and neighbors
            self.update_wall_types_in_area(start_x, y1, start_x, y2)
        elif start_y == end_y:  # Horizontal line
            x1, x2 = min(start_x, end_x), max(start_x, end_x)
            for x in range(x1, x2 + 1):
                if 0 <= x < self.grid_width:
                    self.grid[start_y][x] = '#'  # Place basic wall first
            # Update wall types for the entire line and neighbors
            self.update_wall_types_in_area(x1, start_y, x2, start_y)

    def draw_erase_line(self, start_x, start_y, end_x, end_y):
        """Draw a line of erased cells between two points and handle 2x2 blocks"""
        erased_2x2_blocks = set()  # Track erased 2x2 blocks to avoid duplicate processing
        
        if start_x == end_x:  # Vertical line
            y1, y2 = min(start_y, end_y), max(start_y, end_y)
            for y in range(y1, y2 + 1):
                if 0 <= y < self.grid_height:
                    # Check for 2x2 block first
                    if not self.erase_2x2_block_if_needed(start_x, y):
                        # If not a 2x2 block, erase single cell
                        self.grid[y][start_x] = '.'
                        self.spike_grid[y][start_x] = '.'  # Also erase spike
            # Update wall types for surrounding area
            self.update_wall_types_in_area(start_x - 1, y1 - 1, start_x + 1, y2 + 1)
        elif start_y == end_y:  # Horizontal line
            x1, x2 = min(start_x, end_x), max(start_x, end_x)
            for x in range(x1, x2 + 1):
                if 0 <= x < self.grid_width:
                    # Check for 2x2 block first
                    if not self.erase_2x2_block_if_needed(x, start_y):
                        # If not a 2x2 block, erase single cell
                        self.grid[start_y][x] = '.'
                        self.spike_grid[start_y][x] = '.'  # Also erase spike
            # Update wall types for surrounding area
            self.update_wall_types_in_area(x1 - 1, start_y - 1, x2 + 1, start_y + 1)
                    
    def on_mouse_press(self, event):
        canvas_x = self.canvasx(event.x)
        canvas_y = self.canvasy(event.y)
        grid_x, grid_y = self.get_grid_pos(canvas_x, canvas_y)
        
        if grid_x is not None and grid_y is not None:
            if self.drawing_mode == 'select':
                # Start selection
                self.selection_start = (grid_x, grid_y)
                self.selection_end = (grid_x, grid_y)
                self.selection_active = True
                self.is_drawing = True
                self.update_canvas()
                return
            
            self.is_drawing = True
            self.last_pos = (grid_x, grid_y)
            
            # Reset axis tracking for new drawing session
            self.drawing_axis = None
            self.axis_start_pos = (grid_x, grid_y)
            
            if self.drawing_mode == 'wall':
                self.grid[grid_y][grid_x] = '#'
                # Update this wall and its neighbors with proper wall types
                self.update_wall_types_in_area(grid_x, grid_y, grid_x, grid_y)
            elif self.drawing_mode == 'start':
                self.place_2x2_block(grid_x, grid_y, 'S')
            elif self.drawing_mode == 'finish':
                self.place_2x2_block(grid_x, grid_y, 'F')
            elif self.drawing_mode == 'asterisk':
                # Start manual asterisk path drawing
                self.start_asterisk_drawing()
            elif self.drawing_mode == 'start_path':
                # Mark path direction for Start block
                self.mark_path_direction(grid_x, grid_y, 'start_path')
                return  # Don't set is_drawing for path marking
            elif self.drawing_mode == 'finish_path':
                # Mark path direction for Finish block
                self.mark_path_direction(grid_x, grid_y, 'finish_path')
                return  # Don't set is_drawing for path marking
            elif self.drawing_mode == 'spike1':
                self.spike_grid[grid_y][grid_x] = '!'
                self.grid[grid_y][grid_x] = '#'
                # Update wall types
                self.update_wall_types_in_area(grid_x, grid_y, grid_x, grid_y)
            elif self.drawing_mode == 'spike2':
                self.spike_grid[grid_y][grid_x] = '?'
                self.grid[grid_y][grid_x] = '#'
                # Update wall types
                self.update_wall_types_in_area(grid_x, grid_y, grid_x, grid_y)
            elif self.drawing_mode == 'erase':
                # Check if we're erasing a 2x2 block first
                if not self.erase_2x2_block_if_needed(grid_x, grid_y):
                    # If not a 2x2 block, erase single cell
                    self.grid[grid_y][grid_x] = '.'
                    self.spike_grid[grid_y][grid_x] = '.'  # Also erase spike
                    # Update surrounding walls when erasing
                    self.update_wall_types_in_area(grid_x - 1, grid_y - 1, grid_x + 1, grid_y + 1)
                
            self.update_canvas()
            
    def on_mouse_drag(self, event):
        if self.is_drawing:
            canvas_x = self.canvasx(event.x)
            canvas_y = self.canvasy(event.y)
            grid_x, grid_y = self.get_grid_pos(canvas_x, canvas_y)
            
            if grid_x is not None and grid_y is not None:
                if self.drawing_mode == 'select':
                    # Update selection end point
                    self.selection_end = (grid_x, grid_y)
                    self.update_canvas()
                    return
                
                if self.last_pos:
                    if self.drawing_mode == 'wall':
                        # Implement axis snapping for walls
                        if self.axis_start_pos:
                            start_x, start_y = self.axis_start_pos
                            
                            # Determine axis if not already set
                            if self.drawing_axis is None:
                                dx = abs(grid_x - start_x)
                                dy = abs(grid_y - start_y)
                                
                                # Only set axis if we've moved at least one cell
                                if dx > 0 or dy > 0:
                                    if dx >= dy:
                                        self.drawing_axis = 'horizontal'
                                    else:
                                        self.drawing_axis = 'vertical'
                            
                            # Snap to axis
                            if self.drawing_axis == 'horizontal':
                                grid_y = start_y  # Lock Y coordinate
                            elif self.drawing_axis == 'vertical':
                                grid_x = start_x  # Lock X coordinate
                        
                        # Draw line from last position to current position
                        self.draw_wall_line(self.last_pos[0], self.last_pos[1], grid_x, grid_y)
                        self.last_pos = (grid_x, grid_y)
                        
                    elif self.drawing_mode in ['spike1', 'spike2']:
                        # Implement axis snapping for spikes (same as walls)
                        if self.axis_start_pos:
                            start_x, start_y = self.axis_start_pos
                            
                            # Determine axis if not already set
                            if self.drawing_axis is None:
                                dx = abs(grid_x - start_x)
                                dy = abs(grid_y - start_y)
                                
                                # Only set axis if we've moved at least one cell
                                if dx > 0 or dy > 0:
                                    if dx >= dy:
                                        self.drawing_axis = 'horizontal'
                                    else:
                                        self.drawing_axis = 'vertical'
                            
                            # Snap to axis
                            if self.drawing_axis == 'horizontal':
                                grid_y = start_y  # Lock Y coordinate
                            elif self.drawing_axis == 'vertical':
                                grid_x = start_x  # Lock X coordinate
                        
                        # Draw spike line from last position to current position
                        self.draw_spike_line(self.last_pos[0], self.last_pos[1], grid_x, grid_y)
                        self.last_pos = (grid_x, grid_y)
                        
                    elif self.drawing_mode == 'erase':
                        # Implement axis snapping for eraser (same as walls)
                        if self.axis_start_pos:
                            start_x, start_y = self.axis_start_pos
                            
                            # Determine axis if not already set
                            if self.drawing_axis is None:
                                dx = abs(grid_x - start_x)
                                dy = abs(grid_y - start_y)
                                
                                # Only set axis if we've moved at least one cell
                                if dx > 0 or dy > 0:
                                    if dx >= dy:
                                        self.drawing_axis = 'horizontal'
                                    else:
                                        self.drawing_axis = 'vertical'
                            
                            # Snap to axis
                            if self.drawing_axis == 'horizontal':
                                grid_y = start_y  # Lock Y coordinate
                            elif self.drawing_axis == 'vertical':
                                grid_x = start_x  # Lock X coordinate
                        
                        # Draw erase line from last position to current position
                        self.draw_erase_line(self.last_pos[0], self.last_pos[1], grid_x, grid_y)
                        self.last_pos = (grid_x, grid_y)
                    
                self.update_canvas()
                
    def on_mouse_release(self, event):
        if self.is_drawing:
            # Save state after drawing action
            self.save_state()
            # Update undo/redo buttons in parent
            if self.map_drawer:
                self.map_drawer.update_undo_redo_buttons()
                # Update selection buttons if in select mode
                if self.drawing_mode == 'select':
                    self.map_drawer.update_selection_buttons()
            
        self.is_drawing = False
        self.last_pos = None
        # Reset axis tracking when mouse is released
        self.drawing_axis = None
        self.axis_start_pos = None
        
    def on_key_press(self, event):
        """Handle key presses for asterisk drawing"""
        if not self.asterisk_drawing_active:
            return
        
        # Map key symbols to directions
        key_to_direction = {
            'Up': 'up',
            'Down': 'down', 
            'Left': 'left',
            'Right': 'right'
        }
        
        if event.keysym in key_to_direction:
            direction = key_to_direction[event.keysym]
            self.move_asterisk_cursor(direction)
        elif event.keysym == 'Escape':
            # Exit asterisk drawing mode
            self.finish_asterisk_drawing()
    
    def set_zoom(self, zoom_level):
        """Set the zoom level and update the canvas"""
        # Clamp zoom level between 0.25x and 4x
        self.zoom_level = max(0.25, min(4.0, zoom_level))
        
        # Update cell size based on zoom - ensure it's always even for proper 2x2 guide grid alignment
        raw_cell_size = self.base_cell_size * self.zoom_level
        self.cell_size = int(raw_cell_size)
        # Ensure cell_size is even for proper 2x2 guide grid alignment
        if self.cell_size % 2 != 0:
            self.cell_size += 1
        
        # Update canvas size and scroll region
        canvas_width = self.grid_width * self.cell_size
        canvas_height = self.grid_height * self.cell_size
        
        self.configure(scrollregion=(0, 0, canvas_width, canvas_height))
        
        # Redraw the canvas
        self.update_canvas()
        
        # Update zoom display in parent
        if self.map_drawer:
            zoom_percent = int(self.zoom_level * 100)
            self.map_drawer.zoom_var.set(f"{zoom_percent}%")
    
    def zoom_in(self):
        """Increase zoom level"""
        new_zoom = self.zoom_level * 1.25
        self.set_zoom(new_zoom)
    
    def zoom_out(self):
        """Decrease zoom level"""
        new_zoom = self.zoom_level / 1.25
        self.set_zoom(new_zoom)
    
    def zoom_reset(self):
        """Reset zoom to 100%"""
        self.set_zoom(1.0)
    
    def get_zoom_level(self):
        """Get current zoom level"""
        return self.zoom_level
    
    def update_canvas(self):
        """Redraw the entire canvas"""
        self.delete("all")
        
        # Calculate canvas size
        canvas_width = self.grid_width * self.cell_size
        canvas_height = self.grid_height * self.cell_size
        
        # Update scroll region
        self.configure(scrollregion=(0, 0, canvas_width, canvas_height))
        
        # Draw main grid lines
        for i in range(self.grid_width + 1):
            x = i * self.cell_size
            self.create_line(x, 0, x, canvas_height, fill='lightgray', width=1)
            
        for i in range(self.grid_height + 1):
            y = i * self.cell_size
            self.create_line(0, y, canvas_width, y, fill='lightgray', width=1)
        
        # Draw grid contents
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                cell_value = self.grid[y][x]
                if cell_value != '.':
                    x1 = x * self.cell_size
                    y1 = y * self.cell_size
                    x2 = x1 + self.cell_size
                    y2 = y1 + self.cell_size
                    
                    if cell_value == '#' or cell_value in '123456789':
                        # Check if this wall is also a spike
                        spike_value = self.spike_grid[y][x]
                        if spike_value in ['!', '?']:
                            # Render spike with cyan background
                            self.create_rectangle(x1, y1, x2, y2, fill='cyan', outline='cyan')
                            self.create_text(x1 + self.cell_size//2, y1 + self.cell_size//2, 
                                           text=spike_value, fill='black', font=('Arial', 10, 'bold'))
                        else:
                            # Regular wall - all wall types get black background
                            self.create_rectangle(x1, y1, x2, y2, fill='black', outline='black')
                            # Display the wall type number or # for basic walls
                            display_text = cell_value if cell_value != '#' else '#'
                            self.create_text(x1 + self.cell_size//2, y1 + self.cell_size//2, 
                                           text=display_text, fill='white', font=('Arial', 8, 'bold'))
                    elif cell_value == 'S':
                        self.create_rectangle(x1, y1, x2, y2, fill='green', outline='green')
                        self.create_text(x1 + self.cell_size//2, y1 + self.cell_size//2, 
                                       text='S', fill='white', font=('Arial', 10, 'bold'))
                    elif cell_value == 'F':
                        self.create_rectangle(x1, y1, x2, y2, fill='red', outline='red')
                        self.create_text(x1 + self.cell_size//2, y1 + self.cell_size//2, 
                                       text='F', fill='white', font=('Arial', 10, 'bold'))
                    elif cell_value == '*':
                        # Asterisk blocks (purple color) - just purple cells, no arrows
                        self.create_rectangle(x1, y1, x2, y2, fill='purple', outline='purple')
                    elif cell_value == ',':
                        # Yellow accessible areas (flood-filled from start)
                        self.create_rectangle(x1, y1, x2, y2, fill='yellow', outline='orange')
                        self.create_text(x1 + self.cell_size//2, y1 + self.cell_size//2, 
                                       text='', fill='black', font=('Arial', 8))
        
        # Draw selection rectangle if active
        if self.selection_active and self.selection_start and self.selection_end:
            bounds = self.get_selection_bounds()
            if bounds:
                min_x, min_y, max_x, max_y = bounds
                sel_x1 = min_x * self.cell_size
                sel_y1 = min_y * self.cell_size
                sel_x2 = (max_x + 1) * self.cell_size
                sel_y2 = (max_y + 1) * self.cell_size
                
                # Draw selection rectangle with dashed lines
                self.create_rectangle(sel_x1, sel_y1, sel_x2, sel_y2, 
                                    outline='blue', width=2, stipple='gray25')
        
        # Draw asterisk cursor if in drawing mode
        if self.asterisk_drawing_active and self.asterisk_cursor:
            cursor_x, cursor_y = self.asterisk_cursor
            
            # Draw 2x2 cursor outline
            for dy in range(2):
                for dx in range(2):
                    cell_x = cursor_x + dx
                    cell_y = cursor_y + dy
                    
                    if cell_x < self.grid_width and cell_y < self.grid_height:
                        cursor_x1 = cell_x * self.cell_size
                        cursor_y1 = cell_y * self.cell_size
                        cursor_x2 = cursor_x1 + self.cell_size
                        cursor_y2 = cursor_y1 + self.cell_size
                        
                        # Draw cursor outline for each cell in the 2x2 block
                        self.create_rectangle(cursor_x1, cursor_y1, cursor_x2, cursor_y2, 
                                            outline='lime', width=3)
        
        # Draw path marks for Start and Finish blocks
        self.draw_path_marks()
        
        # Draw spike2 overlays (yellow highlights for accessible cells near spike2)
        self.draw_spike2_overlays()
        
        # Draw 2x2 guide grid on top if enabled (always visible, drawn last to be on top)
        if self.show_guide_grid:
            # Calculate canvas size for guide lines
            canvas_width = self.grid_width * self.cell_size
            canvas_height = self.grid_height * self.cell_size
            
            # Vertical guide lines (every 2 cells) - use thicker lines and ensure they're on top
            for i in range(0, self.grid_width + 1, 2):
                x = i * self.cell_size
                self.create_line(x, 0, x, canvas_height, fill='blue', width=3, tags='guide_grid')
                
            # Horizontal guide lines (every 2 cells) - use thicker lines and ensure they're on top
            for i in range(0, self.grid_height + 1, 2):
                y = i * self.cell_size
                self.create_line(0, y, canvas_width, y, fill='blue', width=3, tags='guide_grid')
            
            # Ensure guide grid is always on top
            self.tag_raise('guide_grid')
                    
    def draw_path_marks(self):
        """Draw colored overlays for marked path directions on Start and Finish blocks"""
        
        # Draw Start path marks (green overlay)
        start_pos = self.find_block_position('S')
        if start_pos and self.start_path_marks:
            self.draw_path_marks_for_block(start_pos, self.start_path_marks, 'lightgreen', 'Start Entry')
        
        # Draw Finish path marks (red overlay)
        finish_pos = self.find_block_position('F')
        if finish_pos and self.finish_path_marks:
            self.draw_path_marks_for_block(finish_pos, self.finish_path_marks, 'lightcoral', 'Finish Exit')
    
    def draw_path_marks_for_block(self, block_pos, path_marks, color, label_prefix):
        """Draw path marks extending from a specific block until hitting walls or grid edges"""
        block_x, block_y = block_pos
        
        # Direction vectors for movement
        direction_vectors = {
            'top': (0, -1),
            'bottom': (0, 1),
            'left': (-1, 0),
            'right': (1, 0)
        }
        
        # Direction symbols for arrows
        direction_symbols = {
            'top': '↑',
            'bottom': '↓', 
            'left': '←',
            'right': '→'
        }
        
        for direction in path_marks:
            if direction in direction_vectors:
                dx, dy = direction_vectors[direction]
                
                # Find the first open cell in each direction from the 2x2 block
                search_positions = []
                if direction == 'top':
                    search_positions = [(block_x, block_y - 1), (block_x + 1, block_y - 1)]
                elif direction == 'bottom':
                    search_positions = [(block_x, block_y + 2), (block_x + 1, block_y + 2)]
                elif direction == 'left':
                    search_positions = [(block_x - 1, block_y), (block_x - 1, block_y + 1)]
                elif direction == 'right':
                    search_positions = [(block_x + 2, block_y), (block_x + 2, block_y + 1)]
                
                # For each search position, draw overlays for walls and then open cells
                for search_x, search_y in search_positions:
                    current_x, current_y = search_x, search_y
                    first_open_found = False
                    
                    # First, draw overlays on walls
                    while (0 <= current_x < self.grid_width and 
                           0 <= current_y < self.grid_height):
                        
                        cell_content = self.grid[current_y][current_x]
                        
                        # If it's a wall, draw overlay with no arrow
                        if cell_content in ['#', '1', '2', '3', '4', '5', '6', '7', '8', '9']:
                            x1 = current_x * self.cell_size
                            y1 = current_y * self.cell_size
                            x2 = x1 + self.cell_size
                            y2 = y1 + self.cell_size
                            
                            # Use different colors for walls based on start vs finish path
                            if 'start' in label_prefix.lower():
                                wall_color = 'lightgreen'
                            else:  # finish path
                                wall_color = 'lightcoral'
                            
                            # Draw colored overlay on walls
                            self.create_rectangle(x1, y1, x2, y2, 
                                                fill=wall_color, outline=wall_color, stipple='gray50')
                        
                        # If we find an open cell, start the colored overlay from here
                        elif cell_content == '.':
                            first_open_found = True
                            break
                        
                        # Move to next position in the search direction
                        current_x += dx
                        current_y += dy
                    
                    # If we found an open cell, draw the colored overlay extending from there
                    if first_open_found:
                        first_open_cell = True
                        
                        # Continue extending the colored overlay until we hit a wall or boundary
                        while (0 <= current_x < self.grid_width and 
                               0 <= current_y < self.grid_height and
                               self.grid[current_y][current_x] == '.'):
                            
                            x1 = current_x * self.cell_size
                            y1 = current_y * self.cell_size
                            x2 = x1 + self.cell_size
                            y2 = y1 + self.cell_size
                            
                            # Draw colored overlay for open cells
                            self.create_rectangle(x1, y1, x2, y2, 
                                                fill=color, outline=color, stipple='gray50')
                            
                            # Add direction arrow on the first open cell of the overlay
                            if first_open_cell:
                                # For start path, flip the arrow to point toward the start block
                                if 'start' in label_prefix.lower():
                                    # Flip the direction for start path arrows
                                    flipped_directions = {
                                        'top': '↓',    # Arrow points down toward start
                                        'bottom': '↑', # Arrow points up toward start  
                                        'left': '→',   # Arrow points right toward start
                                        'right': '←'   # Arrow points left toward start
                                    }
                                    symbol = flipped_directions[direction]
                                else:
                                    # For finish path, keep original direction (pointing away)
                                    symbol = direction_symbols[direction]
                                
                                self.create_text(x1 + self.cell_size//2, y1 + self.cell_size//2, 
                                               text=symbol, fill='black', font=('Arial', 12, 'bold'))
                                first_open_cell = False
                            
                            # Move to next position in the direction
                            current_x += dx
                            current_y += dy
                    
    def get_map_data(self):
        """Get the grid data as a list of strings"""
        return [''.join(row) for row in self.grid]
    
    def get_content_bounds(self):
        """
        Find the bounds of all meaningful content in the grid based on 2x2 chunks
        If a 2x2 chunk contains any non-dot content, include that chunk
        Returns (min_x, min_y, max_x, max_y) or None if no content found
        """
        # Find which 2x2 chunks contain content
        chunk_width = self.grid_width // 2
        chunk_height = self.grid_height // 2
        
        min_chunk_x = chunk_width
        min_chunk_y = chunk_height
        max_chunk_x = -1
        max_chunk_y = -1
        
        content_found = False
        
        # Check each 2x2 chunk
        for chunk_y in range(chunk_height):
            for chunk_x in range(chunk_width):
                # Check if this 2x2 chunk has any non-dot content
                chunk_has_content = False
                
                for dy in range(2):
                    for dx in range(2):
                        grid_x = chunk_x * 2 + dx
                        grid_y = chunk_y * 2 + dy
                        
                        if (grid_x < self.grid_width and grid_y < self.grid_height and 
                            self.grid[grid_y][grid_x] != '.'):
                            chunk_has_content = True
                            break
                    
                    if chunk_has_content:
                        break
                
                # If this chunk has content, include it in bounds
                if chunk_has_content:
                    min_chunk_x = min(min_chunk_x, chunk_x)
                    min_chunk_y = min(min_chunk_y, chunk_y)
                    max_chunk_x = max(max_chunk_x, chunk_x)
                    max_chunk_y = max(max_chunk_y, chunk_y)
                    content_found = True
        
        # Return None if no content found
        if not content_found:
            return None
        
        # Convert chunk coordinates back to grid coordinates
        min_x = min_chunk_x * 2
        min_y = min_chunk_y * 2
        max_x = (max_chunk_x + 1) * 2 - 1  # End of the chunk
        max_y = (max_chunk_y + 1) * 2 - 1  # End of the chunk
        
        return (min_x, min_y, max_x, max_y)
    
    def get_cropped_map_data(self):
        """
        Get the grid data cropped to the minimum bounding box that contains all content,
        aligned to 2x2 chunk boundaries
        """
        bounds = self.get_content_bounds()
        
        # If no content, return a minimal 2x2 grid
        if bounds is None:
            return ['..', '..'], 2, 2
        
        min_x, min_y, max_x, max_y = bounds
        
        # Align to 2x2 chunk boundaries (round down for min, round up for max)
        crop_min_x = (min_x // 2) * 2
        crop_min_y = (min_y // 2) * 2
        crop_max_x = ((max_x + 1) // 2) * 2 - 1  # -1 because max is inclusive
        crop_max_y = ((max_y + 1) // 2) * 2 - 1
        
        # Ensure we don't go outside the grid
        crop_min_x = max(0, crop_min_x)
        crop_min_y = max(0, crop_min_y)
        crop_max_x = min(self.grid_width - 1, crop_max_x)
        crop_max_y = min(self.grid_height - 1, crop_max_y)
        
        # Calculate cropped dimensions
        crop_width = crop_max_x - crop_min_x + 1
        crop_height = crop_max_y - crop_min_y + 1
        
        # Extract the cropped data
        cropped_data = []
        for y in range(crop_min_y, crop_min_y + crop_height):
            row = ''.join(self.grid[y][crop_min_x:crop_min_x + crop_width])
            cropped_data.append(row)
        
        return cropped_data, crop_width, crop_height, crop_min_x, crop_min_y

    def get_cropped_spike_data(self, crop_min_x, crop_min_y, crop_width, crop_height):
        """
        Get the spike grid data cropped to the same dimensions as the map data
        """
        cropped_spike_data = []
        for y in range(crop_min_y, crop_min_y + crop_height):
            row = ''.join(self.spike_grid[y][crop_min_x:crop_min_x + crop_width])
            cropped_spike_data.append(row)
        
        return cropped_spike_data

    def save_state(self):
        """Save current grid state and asterisk path state to history"""
        # Create a deep copy of the current grid
        current_grid = [row[:] for row in self.grid]
        current_spike_grid = [row[:] for row in self.spike_grid]
        
        # Save asterisk-related state
        asterisk_state = {
            'cursor': self.asterisk_cursor,
            'path': self.asterisk_path[:],  # Copy the list
            'directions': {k: v[:] for k, v in self.asterisk_directions.items()},  # Deep copy
            'direction_sequence': self.asterisk_direction_sequence[:],  # Copy the list
            'drawing_active': self.asterisk_drawing_active,
            'last_direction': self.asterisk_last_direction
        }
        
        # Save path marks state
        path_marks_state = {
            'start_path_marks': self.start_path_marks[:],  # Copy the list
            'finish_path_marks': self.finish_path_marks[:]  # Copy the list
        }
        
        # Combine grid, spike_grid, asterisk, and path marks state
        current_state = {
            'grid': current_grid,
            'spike_grid': current_spike_grid,
            'asterisk': asterisk_state,
            'path_marks': path_marks_state
        }
        
        # Remove any states after current index (for redo after undo)
        if self.history_index < len(self.history) - 1:
            self.history = self.history[:self.history_index + 1]
        
        # Add new state
        self.history.append(current_state)
        self.history_index += 1
        
        # Limit history size
        if len(self.history) > self.max_history:
            self.history.pop(0)
            self.history_index -= 1
    
    def undo(self):
        """Undo the last action"""
        if self.history_index > 0:
            self.history_index -= 1
            state = self.history[self.history_index]
            
            # Handle both old format (just grid) and new format (grid + asterisk + path_marks)
            if isinstance(state, dict):
                # New format with asterisk and path marks state
                self.grid = [row[:] for row in state['grid']]
                # Handle spike_grid if present
                if 'spike_grid' in state:
                    self.spike_grid = [row[:] for row in state['spike_grid']]
                else:
                    # Initialize empty spike_grid for backward compatibility
                    self.spike_grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
                self.restore_asterisk_state(state['asterisk'])
                # Restore path marks if present (for backward compatibility)
                if 'path_marks' in state:
                    self.restore_path_marks_state(state['path_marks'])
                else:
                    # Reset path marks for older saves
                    self.start_path_marks = []
                    self.finish_path_marks = []
            else:
                # Old format (just grid) - for backward compatibility
                self.grid = [row[:] for row in state]
                # Initialize empty spike_grid for old saves
                self.spike_grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
                # Reset all non-grid state for old saves
                self.asterisk_cursor = None
                self.asterisk_drawing_active = False
                self.start_path_marks = []
                self.finish_path_marks = []
            
            self.update_canvas()
            return True
        return False
    
    def redo(self):
        """Redo the next action"""
        if self.history_index < len(self.history) - 1:
            self.history_index += 1
            state = self.history[self.history_index]
            
            # Handle both old format (just grid) and new format (grid + asterisk + path_marks)
            if isinstance(state, dict):
                # New format with asterisk and path marks state
                self.grid = [row[:] for row in state['grid']]
                # Handle spike_grid if present
                if 'spike_grid' in state:
                    self.spike_grid = [row[:] for row in state['spike_grid']]
                else:
                    # Initialize empty spike_grid for backward compatibility
                    self.spike_grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
                self.restore_asterisk_state(state['asterisk'])
                # Restore path marks if present (for backward compatibility)
                if 'path_marks' in state:
                    self.restore_path_marks_state(state['path_marks'])
                else:
                    # Reset path marks for older saves
                    self.start_path_marks = []
                    self.finish_path_marks = []
            else:
                # Old format (just grid) - for backward compatibility
                self.grid = [row[:] for row in state]
                # Initialize empty spike_grid for old saves
                self.spike_grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
                # Reset all non-grid state for old saves
                self.asterisk_cursor = None
                self.asterisk_drawing_active = False
                self.start_path_marks = []
                self.finish_path_marks = []
            
            self.update_canvas()
            return True
        return False
    
    def restore_asterisk_state(self, asterisk_state):
        """Restore asterisk-related state from history"""
        self.asterisk_cursor = asterisk_state['cursor']
        self.asterisk_path = asterisk_state['path'][:]  # Copy the list
        self.asterisk_directions = {k: v[:] for k, v in asterisk_state['directions'].items()}  # Deep copy
        self.asterisk_direction_sequence = asterisk_state['direction_sequence'][:]  # Copy the list
        self.asterisk_drawing_active = asterisk_state['drawing_active']
        self.asterisk_last_direction = asterisk_state['last_direction']
        
        # Update UI state if needed
        if self.map_drawer and self.asterisk_drawing_active:
            # Ensure canvas can receive keyboard focus for asterisk drawing
            self.focus_set()
            # Update status to reflect restored state
            if self.asterisk_cursor:
                self.map_drawer.status_var.set("Asterisk drawing mode restored. Use arrow keys to continue, ESC to finish.")
    
    def restore_path_marks_state(self, path_marks_state):
        """Restore path marks state from history"""
        self.start_path_marks = path_marks_state['start_path_marks'][:]  # Copy the list
        self.finish_path_marks = path_marks_state['finish_path_marks'][:]  # Copy the list
    
    def can_undo(self):
        """Check if undo is possible"""
        return self.history_index > 0
    
    def can_redo(self):
        """Check if redo is possible"""
        return self.history_index < len(self.history) - 1

    def get_selection_bounds(self):
        """Get the normalized bounds of the current selection"""
        if not self.selection_start or not self.selection_end:
            return None
            
        x1, y1 = self.selection_start
        x2, y2 = self.selection_end
        
        # Normalize coordinates
        min_x, max_x = min(x1, x2), max(x1, x2)
        min_y, max_y = min(y1, y2), max(y1, y2)
        
        return min_x, min_y, max_x, max_y
    
    def copy_selection(self):
        """Copy the selected area to clipboard"""
        bounds = self.get_selection_bounds()
        if not bounds:
            return False
            
        min_x, min_y, max_x, max_y = bounds
        
        # Copy the selected area
        copied_data = []
        for y in range(min_y, max_y + 1):
            row = []
            for x in range(min_x, max_x + 1):
                if 0 <= y < self.grid_height and 0 <= x < self.grid_width:
                    row.append(self.grid[y][x])
                else:
                    row.append('.')
            copied_data.append(row)
        
        self.clipboard = {
            'data': copied_data,
            'width': max_x - min_x + 1,
            'height': max_y - min_y + 1
        }
        
        # Update parent's selection buttons
        if self.map_drawer:
            self.map_drawer.update_selection_buttons()
            
        return True
    
    def paste_selection(self, paste_x, paste_y):
        """Paste clipboard data at the specified position"""
        if not self.clipboard:
            return False
            
        data = self.clipboard['data']
        
        for dy, row in enumerate(data):
            for dx, cell in enumerate(row):
                target_x = paste_x + dx
                target_y = paste_y + dy
                
                if 0 <= target_y < self.grid_height and 0 <= target_x < self.grid_width:
                    self.grid[target_y][target_x] = cell
        
        self.save_state()
        self.update_canvas()
        return True
    
    def delete_selection(self):
        """Delete (clear) the selected area"""
        bounds = self.get_selection_bounds()
        if not bounds:
            return False
            
        min_x, min_y, max_x, max_y = bounds
        
        for y in range(min_y, max_y + 1):
            for x in range(min_x, max_x + 1):
                if 0 <= y < self.grid_height and 0 <= x < self.grid_width:
                    self.grid[y][x] = '.'
        
        self.save_state()
        self.update_canvas()
        return True
    
    def clear_selection(self):
        """Clear the current selection"""
        self.selection_start = None
        self.selection_end = None
        self.selection_active = False
        self.update_canvas()
        
        # Update parent's selection buttons
        if self.map_drawer:
            self.map_drawer.update_selection_buttons()

    def convert_legacy_walls(self):
        """Convert old # walls to basic walls, then use flood fill for orientation"""
        # First pass: convert all numbered walls back to # (reset to basic walls)
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                if self.grid[y][x] in '123456789':
                    self.grid[y][x] = '#'
        
        # Run flood fill from start position to mark accessible areas
        self.flood_fill_from_start()
        
        # Update wall orientations based on accessible areas
        self.update_wall_types_from_flood()
    
    def set_map_data(self, map_lines):
        """Set grid from list of strings, converting legacy formats"""
        self.grid_height = len(map_lines)
        if self.grid_height > 0:
            self.grid_width = len(map_lines[0])
        
        # Initialize grid
        self.grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
        
        # Load the map data
        for y, line in enumerate(map_lines):
            for x, char in enumerate(line):
                if x < self.grid_width and y < self.grid_height:
                    self.grid[y][x] = char
        
        # Convert legacy walls to smart wall system
        self.convert_legacy_walls()
        
        # Update canvas size
        self.configure(width=min(800, self.grid_width * self.cell_size),
                      height=min(600, self.grid_height * self.cell_size))
        
        self.save_state()
        self.update_canvas()
    
    def compress_direction_sequence(self, directions):
        """
        Compress a sequence of directions by removing consecutive duplicates
        
        Args:
            directions: List of direction strings
            
        Returns:
            List of compressed directions (consecutive duplicates removed)
        """
        if not directions:
            return []
        
        compressed = [directions[0]]
        
        for direction in directions[1:]:
            if direction != compressed[-1]:
                compressed.append(direction)
        
        return compressed
    
    def set_zoom(self, zoom_level, mouse_x=None, mouse_y=None):
        """Set the zoom level and update the canvas, optionally centering on mouse position"""
        # Find the closest discrete zoom level
        closest_index = min(range(len(self.zoom_levels)), 
                           key=lambda i: abs(self.zoom_levels[i] - zoom_level))
        
        old_zoom = self.zoom_level
        self.zoom_index = closest_index
        self.zoom_level = self.zoom_levels[self.zoom_index]
        
        # Get current view center if no mouse position provided
        if mouse_x is None or mouse_y is None:
            # Get current scroll position and visible area
            canvas_width = self.winfo_width()
            canvas_height = self.winfo_height()
            scroll_x = self.canvasx(canvas_width / 2)
            scroll_y = self.canvasy(canvas_height / 2)
        else:
            # Convert mouse position to canvas coordinates
            scroll_x = self.canvasx(mouse_x)
            scroll_y = self.canvasy(mouse_y)
        
        # Convert canvas coordinates to grid coordinates
        old_cell_size = int(self.base_cell_size * old_zoom)
        if old_cell_size > 0:
            grid_x = scroll_x / old_cell_size
            grid_y = scroll_y / old_cell_size
        else:
            grid_x = grid_y = 0
        
        # Update cell size based on new zoom - ensure it's always even for proper 2x2 guide grid alignment
        raw_cell_size = self.base_cell_size * self.zoom_level
        self.cell_size = int(raw_cell_size)
        # Ensure cell_size is even for proper 2x2 guide grid alignment
        if self.cell_size % 2 != 0:
            self.cell_size += 1
        
        # Update canvas size and scroll region
        canvas_width = self.grid_width * self.cell_size
        canvas_height = self.grid_height * self.cell_size
        self.configure(scrollregion=(0, 0, canvas_width, canvas_height))
        
        # Calculate new scroll position to keep the same grid point under mouse
        new_scroll_x = grid_x * self.cell_size
        new_scroll_y = grid_y * self.cell_size
        
        # Redraw the canvas
        self.update_canvas()
        
        # Update scroll position to maintain zoom center
        if mouse_x is not None and mouse_y is not None:
            # Scroll so the same grid point is under the mouse
            visible_width = self.winfo_width()
            visible_height = self.winfo_height()
            
            target_x = new_scroll_x - mouse_x
            target_y = new_scroll_y - mouse_y
            
            # Convert to scroll fractions
            if canvas_width > visible_width:
                scroll_x_fraction = target_x / (canvas_width - visible_width)
                scroll_x_fraction = max(0, min(1, scroll_x_fraction))
                self.xview_moveto(scroll_x_fraction)
            
            if canvas_height > visible_height:
                scroll_y_fraction = target_y / (canvas_height - visible_height)
                scroll_y_fraction = max(0, min(1, scroll_y_fraction))
                self.yview_moveto(scroll_y_fraction)
        else:
            # Center the view on the calculated position
            visible_width = self.winfo_width()
            visible_height = self.winfo_height()
            
            if canvas_width > visible_width:
                center_x = new_scroll_x - visible_width / 2
                scroll_x_fraction = center_x / (canvas_width - visible_width)
                scroll_x_fraction = max(0, min(1, scroll_x_fraction))
                self.xview_moveto(scroll_x_fraction)
            
            if canvas_height > visible_height:
                center_y = new_scroll_y - visible_height / 2
                scroll_y_fraction = center_y / (canvas_height - visible_height)
                scroll_y_fraction = max(0, min(1, scroll_y_fraction))
                self.yview_moveto(scroll_y_fraction)
        
        # Update zoom display in parent
        if self.map_drawer:
            zoom_percent = int(self.zoom_level * 100)
            self.map_drawer.zoom_var.set(f"{zoom_percent}%")
    
    def zoom_in(self, mouse_x=None, mouse_y=None):
        """Increase zoom level to next discrete level"""
        if self.zoom_index < len(self.zoom_levels) - 1:
            self.zoom_index += 1
            self.set_zoom(self.zoom_levels[self.zoom_index], mouse_x, mouse_y)
    
    def zoom_out(self, mouse_x=None, mouse_y=None):
        """Decrease zoom level to previous discrete level"""
        if self.zoom_index > 0:
            self.zoom_index -= 1
            self.set_zoom(self.zoom_levels[self.zoom_index], mouse_x, mouse_y)
    
    def zoom_reset(self):
        """Reset zoom to 100%"""
        self.zoom_index = 3  # 100% is at index 3
        self.set_zoom(1.0)
    
    def zoom_to_level(self, level_percent):
        """Zoom to specific percentage level"""
        target_level = level_percent / 100.0
        self.set_zoom(target_level)
    
    def get_zoom_level(self):
        """Get current zoom level"""
        return self.zoom_level

    def draw_spike_line(self, x1, y1, x2, y2):
        """Draw a line of spikes from (x1,y1) to (x2,y2)"""
        points = self.get_line_points(x1, y1, x2, y2)
        spike_symbol = '!' if self.drawing_mode == 'spike1' else '?'
        
        for x, y in points:
            if 0 <= x < self.grid_width and 0 <= y < self.grid_height:
                self.spike_grid[y][x] = spike_symbol
                self.grid[y][x] = '#'
        
        # Update wall types for the entire line
        if points:
            min_x = min(x for x, y in points)
            max_x = max(x for x, y in points)
            min_y = min(y for x, y in points)
            max_y = max(y for x, y in points)
            self.update_wall_types_in_area(min_x, min_y, max_x, max_y)
        
        self.update_canvas()

    def draw_spike2_overlays(self):
        """Draw yellow overlays for cells connected to spike2"""
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                if self.spike_grid[y][x] == '?':  # Spike2
                    # Check all 4 directions from spike
                    directions = [(0, -1), (0, 1), (-1, 0), (1, 0)]  # up, down, left, right
                    
                    for dx, dy in directions:
                        check_x, check_y = x + dx, y + dy
                        if (0 <= check_x < self.grid_width and 0 <= check_y < self.grid_height and 
                            self.grid[check_y][check_x] in ['.', ',', 'S', 'F', '*']):
                            
                            # Draw cyan overlay
                            x1, y1 = check_x * self.cell_size, check_y * self.cell_size
                            x2, y2 = x1 + self.cell_size, y1 + self.cell_size
                            self.create_rectangle(x1, y1, x2, y2, fill='cyan', stipple='gray25', 
                                                outline='', tags="spike2_overlay")

    def get_line_points(self, x1, y1, x2, y2):
        """Get all points on a line between two coordinates (Bresenham's line algorithm)"""
        points = []
        
        dx = abs(x2 - x1)
        dy = abs(y2 - y1)
        sx = 1 if x1 < x2 else -1
        sy = 1 if y1 < y2 else -1
        err = dx - dy
        
        x, y = x1, y1
        
        while True:
            points.append((x, y))
            
            if x == x2 and y == y2:
                break
                
            e2 = 2 * err
            if e2 > -dy:
                err -= dy
                x += sx
            if e2 < dx:
                err += dx
                y += sy
        
        return points



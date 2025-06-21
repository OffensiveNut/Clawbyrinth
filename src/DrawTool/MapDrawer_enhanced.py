import tkinter as tk
from tkinter import ttk, filedialog, messagebox
import os
import math

class GridCanvas(tk.Canvas):
    def __init__(self, parent, map_drawer=None, **kwargs):
        super().__init__(parent, **kwargs)
        self.parent_window = parent
        self.map_drawer = map_drawer
        
        # Grid settings (must be even numbers)
        self.cell_size = 20
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
        
        self.update_canvas()
        
    def set_grid_size(self, width, height):
        # Ensure grid sizes are even (multiples of 2)
        self.grid_width = width if width % 2 == 0 else width + 1
        self.grid_height = height if height % 2 == 0 else height + 1
        
        # Recreate grid data
        self.grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
        self.update_canvas()
        
    def set_drawing_mode(self, mode):
        self.drawing_mode = mode
        
    def toggle_guide_grid(self, show):
        self.show_guide_grid = show
        self.update_canvas()
        
    def clear_grid(self):
        self.grid = [['.' for _ in range(self.grid_width)] for _ in range(self.grid_height)]
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
    
    def can_place_asterisk(self, align_x, align_y):
        """Check if asterisk can be placed at the given 2x2 location (must be in yellow area)"""
        # Check if all 4 cells of the 2x2 block are yellow areas or empty spaces that can become yellow
        for dy in range(2):
            for dx in range(2):
                x, y = align_x + dx, align_y + dy
                if x >= self.grid_width or y >= self.grid_height:
                    return False
                cell = self.grid[y][x]
                # Can only place on yellow areas (,) or empty spaces (.) that are accessible
                if cell not in [',', '.']:
                    return False
                # If it's an empty space, it should be reachable from yellow areas
                if cell == '.':
                    # Check if it's adjacent to yellow areas
                    has_yellow_neighbor = False
                    for check_dx, check_dy in [(0, 1), (0, -1), (1, 0), (-1, 0)]:
                        check_x, check_y = x + check_dx, y + check_dy
                        if (0 <= check_x < self.grid_width and 0 <= check_y < self.grid_height and
                            self.grid[check_y][check_x] in [',', 'S', 'F', '*']):
                            has_yellow_neighbor = True
                            break
                    if not has_yellow_neighbor:
                        return False
        return True
    
    def remove_existing_block(self, symbol):
        """Remove all existing blocks of the given symbol type"""
        for y in range(self.grid_height):
            for x in range(self.grid_width):
                if self.grid[y][x] == symbol:
                    self.grid[y][x] = '.'
        
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
                success = self.place_2x2_block(grid_x, grid_y, '*')
                if not success:
                    # Show visual feedback that asterisk can't be placed here
                    if self.map_drawer:
                        self.map_drawer.status_var.set("Asterisk can only be placed in yellow accessible areas!")
            elif self.drawing_mode == 'erase':
                # Check if we're erasing a 2x2 block first
                if not self.erase_2x2_block_if_needed(grid_x, grid_y):
                    # If not a 2x2 block, erase single cell
                    self.grid[grid_y][grid_x] = '.'
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
            
        # Draw 2x2 guide grid if enabled
        if self.show_guide_grid:
            # Vertical guide lines (every 2 cells)
            for i in range(0, self.grid_width + 1, 2):
                x = i * self.cell_size
                self.create_line(x, 0, x, canvas_height, fill='blue', width=2)
                
            # Horizontal guide lines (every 2 cells)
            for i in range(0, self.grid_height + 1, 2):
                y = i * self.cell_size
                self.create_line(0, y, canvas_width, y, fill='blue', width=2)
        
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
                        # All wall types get black background
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
                        # Asterisk blocks (purple color to distinguish from other blocks)
                        self.create_rectangle(x1, y1, x2, y2, fill='purple', outline='purple')
                        self.create_text(x1 + self.cell_size//2, y1 + self.cell_size//2, 
                                       text='*', fill='white', font=('Arial', 10, 'bold'))
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
                    
    def get_map_data(self):
        """Get the grid data as a list of strings"""
        return [''.join(row) for row in self.grid]

    def save_state(self):
        """Save current grid state to history"""
        # Create a deep copy of the current grid
        current_state = [row[:] for row in self.grid]
        
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
            self.grid = [row[:] for row in self.history[self.history_index]]
            self.update_canvas()
            return True
        return False
    
    def redo(self):
        """Redo the next action"""
        if self.history_index < len(self.history) - 1:
            self.history_index += 1
            self.grid = [row[:] for row in self.history[self.history_index]]
            self.update_canvas()
            return True
        return False
    
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

class MapDrawer:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("2D Grid Map Drawer - Enhanced (Tkinter)")
        self.root.geometry("1200x800")
        
        # Initialize UI
        self.init_ui()
        
    def init_ui(self):
        # Main frame
        main_frame = ttk.Frame(self.root)
        main_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)
        
        # Left panel for controls
        control_frame = ttk.Frame(main_frame)
        control_frame.pack(side=tk.LEFT, fill=tk.Y, padx=(0, 10))
        
        # Grid size controls
        size_frame = ttk.LabelFrame(control_frame, text="Grid Size (Even Numbers Only)")
        size_frame.pack(fill=tk.X, pady=(0, 10))
        
        ttk.Label(size_frame, text="Width:").grid(row=0, column=0, sticky='w', padx=5, pady=2)
        self.width_var = tk.IntVar(value=40)
        width_spin = ttk.Spinbox(size_frame, from_=6, to=80, increment=2, width=8, 
                                textvariable=self.width_var)
        width_spin.grid(row=0, column=1, padx=5, pady=2)
        
        ttk.Label(size_frame, text="Height:").grid(row=1, column=0, sticky='w', padx=5, pady=2)
        self.height_var = tk.IntVar(value=30)
        height_spin = ttk.Spinbox(size_frame, from_=6, to=60, increment=2, width=8, 
                                 textvariable=self.height_var)
        height_spin.grid(row=1, column=1, padx=5, pady=2)
        
        update_btn = ttk.Button(size_frame, text="Update Grid Size", 
                               command=self.update_grid_size)
        update_btn.grid(row=2, column=0, columnspan=2, pady=5)
        
        # Drawing mode selection
        mode_frame = ttk.LabelFrame(control_frame, text="Drawing Tools")
        mode_frame.pack(fill=tk.X, pady=(0, 10))
        
        self.drawing_mode = tk.StringVar(value='wall')
        
        self.wall_btn = ttk.Button(mode_frame, text="Wall (#) - Line Drawing [W]")
        self.wall_btn.pack(fill=tk.X, padx=5, pady=2)
        self.wall_btn.configure(command=lambda: self.set_drawing_mode('wall'))
        
        self.start_btn = ttk.Button(mode_frame, text="Start (S) - 2x2 Block [S]")
        self.start_btn.pack(fill=tk.X, padx=5, pady=2)
        self.start_btn.configure(command=lambda: self.set_drawing_mode('start'))
        
        self.finish_btn = ttk.Button(mode_frame, text="Finish (F) - 2x2 Block [F]")
        self.finish_btn.pack(fill=tk.X, padx=5, pady=2)
        self.finish_btn.configure(command=lambda: self.set_drawing_mode('finish'))
        
        self.asterisk_btn = ttk.Button(mode_frame, text="Asterisk (*) - 2x2 Block (Yellow Only) [A]")
        self.asterisk_btn.pack(fill=tk.X, padx=5, pady=2)
        self.asterisk_btn.configure(command=lambda: self.set_drawing_mode('asterisk'))
        
        self.erase_btn = ttk.Button(mode_frame, text="Erase (.) - Line Erasing [E]")
        self.erase_btn.pack(fill=tk.X, padx=5, pady=2)
        self.erase_btn.configure(command=lambda: self.set_drawing_mode('erase'))
        
        self.select_btn = ttk.Button(mode_frame, text="Select - Rectangle Selection [R]")
        self.select_btn.pack(fill=tk.X, padx=5, pady=2)
        self.select_btn.configure(command=lambda: self.set_drawing_mode('select'))
        
        # Options
        options_frame = ttk.LabelFrame(control_frame, text="Options")
        options_frame.pack(fill=tk.X, pady=(0, 10))
        
        self.guide_var = tk.BooleanVar(value=True)
        guide_check = ttk.Checkbutton(options_frame, text="Show 2x2 Guide Grid", 
                                     variable=self.guide_var, command=self.toggle_guide_grid)
        guide_check.pack(anchor='w', padx=5, pady=2)
        
        # Action buttons
        action_frame = ttk.LabelFrame(control_frame, text="Actions")
        action_frame.pack(fill=tk.X, pady=(0, 10))
        
        # Undo/Redo buttons
        undo_redo_frame = ttk.Frame(action_frame)
        undo_redo_frame.pack(fill=tk.X, padx=5, pady=2)
        
        self.undo_btn = ttk.Button(undo_redo_frame, text="Undo [Ctrl+Z]", 
                                  command=self.undo, state='disabled')
        self.undo_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 2))
        
        self.redo_btn = ttk.Button(undo_redo_frame, text="Redo [Ctrl+Y]", 
                                  command=self.redo, state='disabled')
        self.redo_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(2, 0))
        
        # Selection operations frame
        selection_frame = ttk.LabelFrame(action_frame, text="Selection Operations")
        selection_frame.pack(fill=tk.X, padx=5, pady=2)
        
        sel_buttons_frame = ttk.Frame(selection_frame)
        sel_buttons_frame.pack(fill=tk.X, padx=2, pady=2)
        
        self.copy_btn = ttk.Button(sel_buttons_frame, text="Copy [Ctrl+C]", 
                                  command=self.copy_selection, state='disabled')
        self.copy_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 1))
        
        self.paste_btn = ttk.Button(sel_buttons_frame, text="Paste [Ctrl+V]", 
                                   command=self.paste_selection, state='disabled')
        self.paste_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(1, 1))
        
        self.del_btn = ttk.Button(sel_buttons_frame, text="Delete [Del]", 
                                 command=self.delete_selection, state='disabled')
        self.del_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(1, 0))
        
        self.clear_sel_btn = ttk.Button(selection_frame, text="Clear Selection [Esc]", 
                                       command=self.clear_selection_ui)
        self.clear_sel_btn.pack(fill=tk.X, padx=2, pady=2)
        
        ttk.Button(action_frame, text="Clear Grid", 
                  command=self.clear_grid).pack(fill=tk.X, padx=5, pady=2)
        ttk.Button(action_frame, text="Save Map", 
                  command=self.save_map).pack(fill=tk.X, padx=5, pady=2)
        ttk.Button(action_frame, text="Load Map", 
                  command=self.load_map).pack(fill=tk.X, padx=5, pady=2)
        
        # Instructions
        instructions_frame = ttk.LabelFrame(control_frame, text="Instructions")
        instructions_frame.pack(fill=tk.BOTH, expand=True, pady=(0, 10))
        
        instructions_text = tk.Text(instructions_frame, height=12, width=30, wrap=tk.WORD)
        instructions_text.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)
        
        instructions_content = """ENHANCED FEATURES:

• Grid sizes are in multiples of 2
• Blue lines show 2x2 guide grid

WALL MODE:
• Click and drag to draw wall lines
• AXIS SNAPPING: Lines snap to horizontal 
  or vertical based on initial direction
• Once you start drawing in one direction,
  the line is locked to that axis
• Much faster than individual clicks

START/FINISH MODES:
• Click anywhere to place 2x2 blocks
• Auto-aligns to 2x2 grid boundaries
• S = Start, F = Finish

SYMBOLS:
• # = Wall
• S = Start (2x2 block)
• F = Finish (2x2 block)
• . = Empty space

The blue guide lines help you see where 2x2 blocks will be placed."""
        
        instructions_text.insert(tk.END, instructions_content)
        instructions_text.configure(state='disabled')
        
        # Canvas frame with scrollbars
        canvas_frame = ttk.Frame(main_frame)
        canvas_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        
        # Create canvas with scrollbars
        self.canvas = GridCanvas(canvas_frame, map_drawer=self, bg='white')
        
        # Scrollbars
        h_scrollbar = ttk.Scrollbar(canvas_frame, orient=tk.HORIZONTAL, command=self.canvas.xview)
        v_scrollbar = ttk.Scrollbar(canvas_frame, orient=tk.VERTICAL, command=self.canvas.yview)
        
        self.canvas.configure(xscrollcommand=h_scrollbar.set, yscrollcommand=v_scrollbar.set)
        
        # Pack scrollbars and canvas
        h_scrollbar.pack(side=tk.BOTTOM, fill=tk.X)
        v_scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        
        # Status bar
        self.status_var = tk.StringVar(value="Ready - Current tool: Wall (Click and drag to draw lines)")
        status_bar = ttk.Label(self.root, textvariable=self.status_var, relief=tk.SUNKEN)
        status_bar.pack(side=tk.BOTTOM, fill=tk.X)
        
        # Set initial mode
        self.set_drawing_mode('wall')
        
        # Add keyboard shortcuts
        self.setup_keyboard_shortcuts()
        
        # Initial update of undo/redo button states
        self.update_undo_redo_buttons()
    
    def undo(self):
        """Undo the last action"""
        if self.canvas.undo():
            self.status_var.set("Undo successful")
        else:
            self.status_var.set("Nothing to undo")
        self.update_undo_redo_buttons()
    
    def redo(self):
        """Redo the next action"""
        if self.canvas.redo():
            self.status_var.set("Redo successful")
        else:
            self.status_var.set("Nothing to redo")
        self.update_undo_redo_buttons()
    
    def update_undo_redo_buttons(self):
        """Update the state of undo/redo buttons"""
        if self.canvas.can_undo():
            self.undo_btn.configure(state='normal')
        else:
            self.undo_btn.configure(state='disabled')
            
        if self.canvas.can_redo():
            self.redo_btn.configure(state='normal')
        else:
            self.redo_btn.configure(state='disabled')
    
    def update_grid_size(self):
        width = self.width_var.get()
        height = self.height_var.get()
        
        # Ensure even numbers
        if width % 2 != 0:
            width += 1
            self.width_var.set(width)
        if height % 2 != 0:
            height += 1
            self.height_var.set(height)
            
        self.canvas.set_grid_size(width, height)
        self.status_var.set(f"Grid resized to {width}x{height}")
    
    def set_drawing_mode(self, mode):
        self.canvas.set_drawing_mode(mode)
        self.drawing_mode.set(mode)
        
        # Visual feedback for active button
        button_style = {
            'wall': ('lightblue', 'Wall mode (W) - Click and drag to draw wall lines'),
            'start': ('lightgreen', 'Start mode (S) - Click to place 2x2 start block'),
            'finish': ('lightcoral', 'Finish mode (F) - Click to place 2x2 finish block'),
            'asterisk': ('mediumpurple', 'Asterisk mode (A) - Click to place 2x2 asterisk in yellow areas only'),
            'erase': ('lightgray', 'Erase mode (E) - Click and drag to erase lines'),
            'select': ('lightyellow', 'Select mode (R) - Click and drag to select rectangle')
        }
        
        # Reset all buttons
        for btn in [self.wall_btn, self.start_btn, self.finish_btn, self.asterisk_btn, self.erase_btn, self.select_btn]:
            btn.configure(style='TButton')
        
        # Highlight active button (using background color - limited in ttk)
        if mode in button_style:
            self.status_var.set(button_style[mode][1])
            
        # Update selection buttons when switching modes
        if mode == 'select':
            self.update_selection_buttons()
        else:
            # Clear selection when switching away from select mode
            self.canvas.clear_selection()
    
    def toggle_guide_grid(self):
        self.canvas.toggle_guide_grid(self.guide_var.get())
        if self.guide_var.get():
            self.status_var.set("2x2 guide grid enabled")
        else:
            self.status_var.set("2x2 guide grid disabled")
    
    def clear_grid(self):
        if messagebox.askyesno("Clear Grid", "Are you sure you want to clear the entire grid?"):
            self.canvas.clear_grid()
            self.status_var.set("Grid cleared")
    
    def save_map(self):
        filename = filedialog.asksaveasfilename(
            defaultextension=".txt",
            filetypes=[("Text files", "*.txt"), ("All files", "*.*")],
            title="Save Map"
        )
        
        if filename:
            try:
                map_data = self.canvas.get_map_data()
                with open(filename, 'w') as f:
                    # Write grid dimensions
                    f.write(f"# Grid dimensions: {self.canvas.grid_width}x{self.canvas.grid_height}\n")
                    f.write(f"# Symbols: 1-9 = Oriented Walls, S = Start, F = Finish, * = Asterisk, , = Accessible, . = Empty\n")
                    f.write(f"# Start and Finish are 2x2 blocks\n")
                    f.write(f"# Grid uses multiples of 2 for dimensions\n")
                    f.write("\n")
                    
                    # Write the grid
                    for row in map_data:
                        f.write(row + "\n")
                
                messagebox.showinfo("Success", f"Map saved to {filename}")
                self.status_var.set(f"Map saved to {os.path.basename(filename)}")
            except Exception as e:
                messagebox.showerror("Error", f"Failed to save map: {str(e)}")
    
    def load_map(self):
        filename = filedialog.askopenfilename(
            filetypes=[("Text files", "*.txt"), ("All files", "*.*")],
            title="Load Map"
        )
        
        if filename:
            try:
                with open(filename, 'r') as f:
                    lines = f.readlines()
                
                # Filter out comments and empty lines
                map_lines = [line.strip() for line in lines if line.strip() and not line.startswith('#')]
                
                if not map_lines:
                    messagebox.showerror("Error", "No valid map data found in file")
                    return
                
                # Update grid dimensions based on loaded map
                height = len(map_lines)
                width = max(len(line) for line in map_lines) if map_lines else 0
                
                # Ensure even dimensions
                if width % 2 != 0:
                    width += 1
                if height % 2 != 0:
                    height += 1
                
                # Update UI controls
                self.width_var.set(width)
                self.height_var.set(height)
                
                # Update canvas
                self.canvas.set_grid_size(width, height)
                
                # Load the map data
                for row_idx, line in enumerate(map_lines):
                    if row_idx < height:
                        for col_idx, char in enumerate(line):
                            if col_idx < width:
                                if char in ['#', 'S', 'F']:
                                    self.canvas.grid[row_idx][col_idx] = char
                                else:
                                    self.canvas.grid[row_idx][col_idx] = '.'
                
                self.canvas.update_canvas()
                messagebox.showinfo("Success", f"Map loaded from {filename}")
                self.status_var.set(f"Map loaded from {os.path.basename(filename)}")
            except Exception as e:
                messagebox.showerror("Error", f"Failed to load map: {str(e)}")
    
    def setup_keyboard_shortcuts(self):
        """Setup keyboard shortcuts for drawing tools"""
        # Bind keyboard shortcuts (both lowercase and uppercase)
        self.root.bind("<KeyPress-w>", lambda e: self.set_drawing_mode('wall'))
        self.root.bind("<KeyPress-W>", lambda e: self.set_drawing_mode('wall'))
        self.root.bind("<KeyPress-s>", lambda e: self.set_drawing_mode('start'))
        self.root.bind("<KeyPress-S>", lambda e: self.set_drawing_mode('start'))
        self.root.bind("<KeyPress-f>", lambda e: self.set_drawing_mode('finish'))
        self.root.bind("<KeyPress-F>", lambda e: self.set_drawing_mode('finish'))
        self.root.bind("<KeyPress-a>", lambda e: self.set_drawing_mode('asterisk'))
        self.root.bind("<KeyPress-A>", lambda e: self.set_drawing_mode('asterisk'))
        self.root.bind("<KeyPress-e>", lambda e: self.set_drawing_mode('erase'))
        self.root.bind("<KeyPress-E>", lambda e: self.set_drawing_mode('erase'))
        self.root.bind("<KeyPress-r>", lambda e: self.set_drawing_mode('select'))
        self.root.bind("<KeyPress-R>", lambda e: self.set_drawing_mode('select'))
        
        # Undo/Redo shortcuts
        self.root.bind("<Control-z>", lambda e: self.undo())
        self.root.bind("<Control-Z>", lambda e: self.undo())
        self.root.bind("<Control-y>", lambda e: self.redo())
        self.root.bind("<Control-Y>", lambda e: self.redo())
        self.root.bind("<Control-Shift-Z>", lambda e: self.redo())  # Alternative redo
        
        # Selection shortcuts
        self.root.bind("<Control-c>", lambda e: self.copy_selection())
        self.root.bind("<Control-C>", lambda e: self.copy_selection())
        self.root.bind("<Control-v>", lambda e: self.paste_selection())
        self.root.bind("<Control-V>", lambda e: self.paste_selection())
        self.root.bind("<Delete>", lambda e: self.delete_selection())
        self.root.bind("<Escape>", lambda e: self.clear_selection_ui())
        
        # Make sure the root window can receive keyboard focus
        self.root.focus_set()
        
        # Update instructions to include keyboard shortcuts
        self.update_instructions_with_shortcuts()
    
    def update_instructions_with_shortcuts(self):
        """Update the instructions text to include keyboard shortcuts"""
        # Find the instructions text widget and update it
        for widget in self.root.winfo_children():
            if isinstance(widget, ttk.Frame):
                for child in widget.winfo_children():
                    if isinstance(child, ttk.Frame):
                        for grandchild in child.winfo_children():
                            if isinstance(grandchild, ttk.LabelFrame) and str(grandchild['text']) == 'Instructions':
                                for text_widget in grandchild.winfo_children():
                                    if isinstance(text_widget, tk.Text):
                                        text_widget.configure(state='normal')
                                        text_widget.delete(1.0, tk.END)
                                        
                                        instructions_content = """ENHANCED FEATURES:

• Grid sizes are in multiples of 2
• Blue lines show 2x2 guide grid
• Full undo/redo support
• Only one Start and one Finish allowed

KEYBOARD SHORTCUTS:
• W = Wall mode
• S = Start mode  
• F = Finish mode
• A = Asterisk mode
• E = Erase mode
• R = Select mode
• Ctrl+Z = Undo
• Ctrl+Y = Redo
• Ctrl+C = Copy selection
• Ctrl+V = Paste selection
• Delete = Delete selection
• Esc = Clear selection

WALL MODE:
• Click and drag to draw wall lines
• AXIS SNAPPING: Lines snap to horizontal 
  or vertical based on initial direction
• Once you start drawing in one direction,
  the line is locked to that axis
• Much faster than individual clicks

ERASE MODE:
• Click and drag to erase lines
• AXIS SNAPPING: Same behavior as walls
• Erases in straight horizontal/vertical lines
• SMART 2x2 DETECTION: Automatically erases entire
  Start/Finish/Asterisk blocks when clicked
• Perfect for removing wall sections and blocks

SELECT MODE:
• Click and drag to select rectangular areas
• Blue dashed rectangle shows selection
• Copy, paste, or delete selected areas
• Perfect for duplicating maze sections

START/FINISH MODES:
• Click anywhere to place 2x2 blocks
• Auto-aligns to 2x2 grid boundaries
• Only ONE start and ONE finish allowed
• Placing new start/finish removes old one
• S = Start, F = Finish

ASTERISK MODE:
• Click to place 2x2 asterisk blocks
• Can ONLY be placed in yellow accessible areas
• Must be completely within yellow or adjacent areas
• Perfect for marking special locations in maze
• A = Asterisk

UNDO/REDO:
• Up to 50 actions can be undone
• Works with all drawing operations
• Clear grid action can also be undone

SYMBOLS:
• 1-9 = Smart Wall Types (auto-oriented):
  • 1 = Bottom-left corner (facing accessible area)
  • 3 = Bottom-right corner (facing accessible area)
  • 7 = Top-left corner (facing accessible area)
  • 9 = Top-right corner (facing accessible area)
  • 2 = Bottom edge (facing accessible area)
  • 8 = Top edge (facing accessible area)
  • 4 = Left edge (facing accessible area)
  • 6 = Right edge (facing accessible area)
  • 5 = Center/standalone wall
• S = Start (2x2 block) - also treated as accessible area
• F = Finish (2x2 block) - also treated as accessible area
• * = Asterisk (2x2 block) - can only be placed in yellow areas, also treated as accessible area
• , = Accessible area (yellow - flood-filled from Start)
• . = Empty space

FLOOD-FILL SYSTEM:
• Place Start (S) to trigger flood-fill
• Yellow areas + Start/Finish/Asterisk show where player can reach
• Walls automatically orient towards accessible areas
• Start, Finish, and Asterisk blocks all count as accessible areas
• System ensures walls face the "inside" of the maze

The blue guide lines help you see where 2x2 blocks will be placed."""
                                        
                                        text_widget.insert(tk.END, instructions_content)
                                        text_widget.configure(state='disabled')
                                        return
    
    def copy_selection(self):
        """Copy the selected area to clipboard"""
        self.canvas.copy_selection()
        self.update_selection_buttons()
    
    def paste_selection(self):
        """Paste from clipboard to current selection"""
        self.canvas.paste_selection()
        self.update_selection_buttons()
    
    def delete_selection(self):
        """Delete contents of selected area"""
        self.canvas.delete_selection()
        self.update_selection_buttons()
    
    def clear_selection_ui(self):
        """Clear the current selection"""
        self.canvas.clear_selection()
        self.update_selection_buttons()
    
    def update_selection_buttons(self):
        """Update the state of selection operation buttons"""
        # Enable/disable copy and delete buttons based on selection
        has_selection = (self.canvas.selection_start is not None and 
                        self.canvas.selection_end is not None and 
                        self.canvas.selection_active)
        
        # Enable/disable copy and delete based on selection
        state = 'normal' if has_selection else 'disabled'
        self.copy_btn.configure(state=state)
        self.del_btn.configure(state=state)
        
        # Enable/disable paste based on clipboard
        paste_state = 'normal' if hasattr(self.canvas, 'clipboard') and self.canvas.clipboard else 'disabled'
        self.paste_btn.configure(state=paste_state)

    def run(self):
        self.root.mainloop()

if __name__ == "__main__":
    app = MapDrawer()
    app.run()

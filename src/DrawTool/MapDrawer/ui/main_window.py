"""
MapDrawer - Main UI component
Handles the main window, toolbar, controls, and UI interactions
"""

import tkinter as tk
from tkinter import ttk, filedialog, messagebox
import os

from ..core.tools import GridCanvas
from ..utils.tooltip import ToolTip


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
        
        # Use PanedWindow to make the toolbar resizable
        self.paned_window = ttk.PanedWindow(main_frame, orient=tk.HORIZONTAL)
        self.paned_window.pack(fill=tk.BOTH, expand=True)
        
        # Left panel for controls (resizable toolbar with scrolling)
        toolbar_container = ttk.Frame(self.paned_window)
        self.paned_window.add(toolbar_container, weight=0)
        
        # Create a canvas and scrollbar for the toolbar
        toolbar_canvas = tk.Canvas(toolbar_container, width=300, highlightthickness=0)
        toolbar_scrollbar = ttk.Scrollbar(toolbar_container, orient=tk.VERTICAL, command=toolbar_canvas.yview)
        self.scrollable_toolbar = ttk.Frame(toolbar_canvas)
        
        # Configure scrolling
        self.scrollable_toolbar.bind(
            "<Configure>",
            lambda e: toolbar_canvas.configure(scrollregion=toolbar_canvas.bbox("all"))
        )
        
        toolbar_canvas.create_window((0, 0), window=self.scrollable_toolbar, anchor="nw")
        toolbar_canvas.configure(yscrollcommand=toolbar_scrollbar.set)
        
        # Pack the scrollable toolbar
        toolbar_canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        toolbar_scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        
        # Bind mouse wheel to toolbar canvas and all its children
        def _on_toolbar_mousewheel(event):
            toolbar_canvas.yview_scroll(int(-1*(event.delta/120)), "units")
        
        def _on_toolbar_mousewheel_linux(event):
            if event.num == 4:
                toolbar_canvas.yview_scroll(-1, "units")
            elif event.num == 5:
                toolbar_canvas.yview_scroll(1, "units")
        
        def bind_mousewheel_recursive(widget):
            """Recursively bind mousewheel events to widget and all children"""
            widget.bind("<MouseWheel>", _on_toolbar_mousewheel)  # Windows/Mac
            widget.bind("<Button-4>", _on_toolbar_mousewheel_linux)  # Linux
            widget.bind("<Button-5>", _on_toolbar_mousewheel_linux)  # Linux
            for child in widget.winfo_children():
                bind_mousewheel_recursive(child)
        
        # Apply mousewheel binding
        toolbar_canvas.bind("<MouseWheel>", _on_toolbar_mousewheel)
        toolbar_canvas.bind("<Button-4>", _on_toolbar_mousewheel_linux)
        toolbar_canvas.bind("<Button-5>", _on_toolbar_mousewheel_linux)
        
        # We'll bind the scrollable_toolbar and its children after all widgets are created
        self.toolbar_canvas = toolbar_canvas
        self.bind_toolbar_mousewheel = bind_mousewheel_recursive
        
        # Use scrollable_toolbar as the control_frame from now on
        control_frame = self.scrollable_toolbar
        
        # Grid size controls
        size_frame = ttk.LabelFrame(control_frame, text="Grid Size (Even Numbers Only)")
        size_frame.pack(fill=tk.X, pady=(0, 10), padx=5)
        
        # Configure column weights for resizing
        size_frame.columnconfigure(1, weight=1)
        
        ttk.Label(size_frame, text="Width:").grid(row=0, column=0, sticky='w', padx=5, pady=2)
        self.width_var = tk.IntVar(value=40)
        width_spin = ttk.Spinbox(size_frame, from_=6, to=80, increment=2, 
                                textvariable=self.width_var)
        width_spin.grid(row=0, column=1, padx=5, pady=2, sticky='ew')
        
        ttk.Label(size_frame, text="Height:").grid(row=1, column=0, sticky='w', padx=5, pady=2)
        self.height_var = tk.IntVar(value=30)
        height_spin = ttk.Spinbox(size_frame, from_=6, to=60, increment=2, 
                                 textvariable=self.height_var)
        height_spin.grid(row=1, column=1, padx=5, pady=2, sticky='ew')
        
        update_btn = ttk.Button(size_frame, text="Update Grid Size", 
                               command=self.update_grid_size)
        update_btn.grid(row=2, column=0, columnspan=2, pady=5, padx=5, sticky='ew')
        ToolTip(update_btn, "Apply new grid dimensions")
        
        # Zoom controls
        zoom_frame = ttk.LabelFrame(control_frame, text="Zoom Controls")
        zoom_frame.pack(fill=tk.X, pady=(0, 10), padx=5)
        
        zoom_buttons_frame = ttk.Frame(zoom_frame)
        zoom_buttons_frame.pack(fill=tk.X, padx=5, pady=2)
        
        zoom_in_btn = ttk.Button(zoom_buttons_frame, text="Zoom In [+]", 
                  command=self.zoom_in)
        zoom_in_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 2))
        ToolTip(zoom_in_btn, "Increase zoom level")
        
        zoom_out_btn = ttk.Button(zoom_buttons_frame, text="Zoom Out [-]", 
                  command=self.zoom_out)
        zoom_out_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(2, 2))
        ToolTip(zoom_out_btn, "Decrease zoom level")
        
        zoom_reset_btn = ttk.Button(zoom_buttons_frame, text="100% [1]", 
                  command=self.zoom_reset)
        zoom_reset_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(2, 0))
        ToolTip(zoom_reset_btn, "Reset zoom to 100%")
        
        # Additional zoom level buttons
        zoom_levels_frame = ttk.Frame(zoom_frame)
        zoom_levels_frame.pack(fill=tk.X, padx=5, pady=2)
        
        ttk.Button(zoom_levels_frame, text="25%", 
                  command=lambda: self.zoom_to_level(25)).pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 1))
        ttk.Button(zoom_levels_frame, text="50%", 
                  command=lambda: self.zoom_to_level(50)).pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(1, 1))
        ttk.Button(zoom_levels_frame, text="200%", 
                  command=lambda: self.zoom_to_level(200)).pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(1, 1))
        ttk.Button(zoom_levels_frame, text="400%", 
                  command=lambda: self.zoom_to_level(400)).pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(1, 0))
        
        # Zoom level display
        self.zoom_var = tk.StringVar(value="100%")
        zoom_label = ttk.Label(zoom_frame, textvariable=self.zoom_var)
        zoom_label.pack(pady=2)
        
        # Drawing mode selection
        mode_frame = ttk.LabelFrame(control_frame, text="Drawing Tools")
        mode_frame.pack(fill=tk.X, pady=(0, 10), padx=5)
        
        self.drawing_mode = tk.StringVar(value='wall')
        
        self.wall_btn = ttk.Button(mode_frame, text="Wall (W)")
        self.wall_btn.pack(fill=tk.X, padx=5, pady=2)
        self.wall_btn.configure(command=lambda: self.set_drawing_mode('wall'))
        ToolTip(self.wall_btn, "Click and drag to draw wall lines")
        
        self.start_btn = ttk.Button(mode_frame, text="Start (S)")
        self.start_btn.pack(fill=tk.X, padx=5, pady=2)
        self.start_btn.configure(command=lambda: self.set_drawing_mode('start'))
        ToolTip(self.start_btn, "Click to place 2x2 start block")
        
        self.finish_btn = ttk.Button(mode_frame, text="Finish (F)")
        self.finish_btn.pack(fill=tk.X, padx=5, pady=2)
        self.finish_btn.configure(command=lambda: self.set_drawing_mode('finish'))
        ToolTip(self.finish_btn, "Click to place 2x2 finish block")
        
        self.asterisk_btn = ttk.Button(mode_frame, text="Asterisk Path (A)")
        self.asterisk_btn.pack(fill=tk.X, padx=5, pady=2)
        self.asterisk_btn.configure(command=lambda: self.set_drawing_mode('asterisk'))
        ToolTip(self.asterisk_btn, "Use arrow keys to draw path from Start")
        
        self.start_path_btn = ttk.Button(mode_frame, text="Start Path (Q)")
        self.start_path_btn.pack(fill=tk.X, padx=5, pady=2)
        self.start_path_btn.configure(command=lambda: self.set_drawing_mode('start_path'))
        ToolTip(self.start_path_btn, "Click directions around Start block")
        
        self.finish_path_btn = ttk.Button(mode_frame, text="Finish Path (T)")
        self.finish_path_btn.pack(fill=tk.X, padx=5, pady=2)
        self.finish_path_btn.configure(command=lambda: self.set_drawing_mode('finish_path'))
        ToolTip(self.finish_path_btn, "Click directions around Finish block")
        
        self.spike1_btn = ttk.Button(mode_frame, text="Spike 1 (1)")
        self.spike1_btn.pack(fill=tk.X, padx=5, pady=2)
        self.spike1_btn.configure(command=lambda: self.set_drawing_mode('spike1'))
        ToolTip(self.spike1_btn, "Click and drag to draw spike lines (cyan !)")
        
        self.spike2_btn = ttk.Button(mode_frame, text="Spike 2 (2)")
        self.spike2_btn.pack(fill=tk.X, padx=5, pady=2)
        self.spike2_btn.configure(command=lambda: self.set_drawing_mode('spike2'))
        ToolTip(self.spike2_btn, "Click and drag to draw spike lines (cyan ?) - shows cyan overlay")
        
        self.cannon_btn = ttk.Button(mode_frame, text="Cannon (N)")
        self.cannon_btn.pack(fill=tk.X, padx=5, pady=2)
        self.cannon_btn.configure(command=lambda: self.set_drawing_mode('cannon'))
        ToolTip(self.cannon_btn, "Click and drag to draw cannon walls (orange N)")
        
        self.portal_btn = ttk.Button(mode_frame, text="Portal (P)")
        self.portal_btn.pack(fill=tk.X, padx=5, pady=2)
        self.portal_btn.configure(command=lambda: self.set_drawing_mode('portal'))
        ToolTip(self.portal_btn, "Click to place portal (must come in pairs)")
        
        self.fish_btn = ttk.Button(mode_frame, text="Fish (I)")
        self.fish_btn.pack(fill=tk.X, padx=5, pady=2)
        self.fish_btn.configure(command=lambda: self.set_drawing_mode('fish'))
        ToolTip(self.fish_btn, "Click to place fish with aura")
        
        self.erase_btn = ttk.Button(mode_frame, text="Erase (E)")
        self.erase_btn.pack(fill=tk.X, padx=5, pady=2)
        self.erase_btn.configure(command=lambda: self.set_drawing_mode('erase'))
        ToolTip(self.erase_btn, "Click and drag to erase lines")
        
        self.select_btn = ttk.Button(mode_frame, text="Select (R)")
        self.select_btn.pack(fill=tk.X, padx=5, pady=2)
        self.select_btn.configure(command=lambda: self.set_drawing_mode('select'))
        ToolTip(self.select_btn, "Rectangle selection for copy/paste")
        
        # Options
        options_frame = ttk.LabelFrame(control_frame, text="Options")
        options_frame.pack(fill=tk.X, pady=(0, 10), padx=5)
        
        self.guide_var = tk.BooleanVar(value=True)
        guide_check = ttk.Checkbutton(options_frame, text="Show 2x2 Guide Grid", 
                                     variable=self.guide_var, command=self.toggle_guide_grid)
        guide_check.pack(anchor='w', padx=5, pady=2)
        
        # Action buttons
        action_frame = ttk.LabelFrame(control_frame, text="Actions")
        action_frame.pack(fill=tk.X, pady=(0, 10), padx=5)
        
        # Undo/Redo buttons
        undo_redo_frame = ttk.Frame(action_frame)
        
        self.undo_btn = ttk.Button(undo_redo_frame, text="Undo [Ctrl+Z]", 
                                  command=self.undo, state='disabled')
        self.undo_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 2))
        ToolTip(self.undo_btn, "Undo last action")
        
        self.redo_btn = ttk.Button(undo_redo_frame, text="Redo [Ctrl+Y]", 
                                  command=self.redo, state='disabled')
        self.redo_btn.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(2, 0))
        ToolTip(self.redo_btn, "Redo last undone action")
        
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
        
        clear_btn = ttk.Button(action_frame, text="Clear Grid", 
                  command=self.clear_grid)
        clear_btn.pack(fill=tk.X, padx=5, pady=2)
        ToolTip(clear_btn, "Clear all content from grid")
        
        save_btn = ttk.Button(action_frame, text="Save Map", 
                  command=self.save_map)
        save_btn.pack(fill=tk.X, padx=5, pady=2)
        ToolTip(save_btn, "Save current map to file")
        
        load_btn = ttk.Button(action_frame, text="Load Map", 
                  command=self.load_map)
        load_btn.pack(fill=tk.X, padx=5, pady=2)
        ToolTip(load_btn, "Load map from file")
        
        # Canvas frame with scrollbars (added to paned window)
        canvas_frame = ttk.Frame(self.paned_window)
        self.paned_window.add(canvas_frame, weight=1)
        
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
        self.status_var = tk.StringVar(value="Ready - Wall (W)")
        status_bar = ttk.Label(self.root, textvariable=self.status_var, relief=tk.SUNKEN)
        status_bar.pack(side=tk.BOTTOM, fill=tk.X)
        
        # Set initial mode
        self.set_drawing_mode('wall')
        
        # Add keyboard shortcuts
        self.setup_keyboard_shortcuts()
        
        # Initial update of undo/redo button states
        self.update_undo_redo_buttons()
        
        # Bind mousewheel scrolling to toolbar and all its children
        self.bind_toolbar_mousewheel(self.scrollable_toolbar)
        
        # Configure toolbar resizing
        self.configure_toolbar_resizing()
    
    def configure_toolbar_resizing(self):
        """Configure the toolbar to resize its contents when the paned window changes"""
        def on_toolbar_resize(event=None):
            # Get the current width of the toolbar canvas
            canvas_width = self.toolbar_canvas.winfo_width()
            
            # Update the window width to match the canvas width
            if canvas_width > 1:  # Make sure canvas is actually rendered
                # Find the window item containing our scrollable_toolbar
                for item in self.toolbar_canvas.find_all():
                    if self.toolbar_canvas.type(item) == "window":
                        self.toolbar_canvas.itemconfig(item, width=canvas_width)
                        break
        
        # Bind the resize event to the toolbar canvas
        self.toolbar_canvas.bind("<Configure>", on_toolbar_resize)
        
        # Also bind to the paned window for when the sash is moved
        self.paned_window.bind("<Button1-Motion>", lambda e: self.root.after_idle(on_toolbar_resize))
        self.paned_window.bind("<ButtonRelease-1>", lambda e: self.root.after_idle(on_toolbar_resize))
    
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
            'wall': ('lightblue', 'Wall (W) - Line drawing'),
            'start': ('lightgreen', 'Start (S) - 2x2 block'),
            'finish': ('lightcoral', 'Finish (F) - 2x2 block'),
            'asterisk': ('mediumpurple', 'Asterisk (A) - Arrow key drawing'),
            'start_path': ('lightgreen', 'Start Path (Q) - Mark entry points'),
            'finish_path': ('lightcoral', 'Finish Path (T) - Mark exit points'),
            'spike1': ('cyan', 'Spike 1 (1) - Draw spike lines (!)'),
            'spike2': ('cyan', 'Spike 2 (2) - Draw spike lines (?) with cyan overlay'),
            'cannon': ('orange', 'Cannon (N) - Draw cannon walls (orange N)'),
            'portal': ('magenta', 'Portal (P) - Portal placement (must come in pairs)'),
            'fish': ('cyan', 'Fish (I) - Fish with aura'),
            'erase': ('lightgray', 'Erase (E) - Line erasing'),
            'select': ('lightyellow', 'Select (R) - Rectangle selection')
        }
        
        # Reset all buttons
        for btn in [self.wall_btn, self.start_btn, self.finish_btn, self.asterisk_btn, self.start_path_btn, self.finish_path_btn, self.spike1_btn, self.spike2_btn, self.cannon_btn, self.portal_btn, self.fish_btn, self.erase_btn, self.select_btn]:
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
    
    def zoom_in(self):
        """Zoom in the map"""
        self.canvas.zoom_in()
        zoom_percent = int(self.canvas.get_zoom_level() * 100)
        self.status_var.set(f"Zoomed in to {zoom_percent}%")
    
    def zoom_out(self):
        """Zoom out the map"""
        self.canvas.zoom_out()
        zoom_percent = int(self.canvas.get_zoom_level() * 100)
        self.status_var.set(f"Zoomed out to {zoom_percent}%")
    
    def zoom_reset(self):
        """Reset zoom to 100%"""
        self.canvas.zoom_reset()
        self.status_var.set("Zoom reset to 100%")
    
    def zoom_to_level(self, level_percent):
        """Zoom to specific percentage level"""
        self.canvas.zoom_to_level(level_percent)
        self.status_var.set(f"Zoom set to {level_percent}%")
    
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
                # Get cropped map data that only includes content areas
                cropped_data, crop_width, crop_height, crop_min_x, crop_min_y = self.canvas.get_cropped_map_data()
                
                # Get cropped spike/trap layer data using the same bounds
                cropped_spike_data = self.canvas.get_cropped_spike_data(crop_min_x, crop_min_y, crop_width, crop_height)
                
                with open(filename, 'w') as f:
                    # Write grid dimensions (using cropped dimensions)
                    f.write(f"# Grid dimensions: {crop_width}x{crop_height}\n")
                    f.write(f"# Symbols: 1-9 = Oriented Walls, S = Start, F = Finish, * = Asterisk, , = Accessible, . = Empty\n")
                    f.write(f"# Trap Layer: ! = Spike 1, ? = Spike 2, N = Cannon, . = Empty\n")
                    f.write("\n")
                    
                    # Write the cropped map layer
                    for row in cropped_data:
                        f.write(row + "\n")
                    
                    # Write separator and trap layer
                    f.write("\n")
                    f.write("# Trap Layer\n")
                    for row in cropped_spike_data:
                        f.write(row + "\n")
                    
                    # Write direction data if asterisk path exists
                    if self.canvas.asterisk_direction_sequence:
                        f.write("\n")
                        # Compress the direction sequence to remove consecutive duplicates
                        compressed_directions = self.canvas.compress_direction_sequence(self.canvas.asterisk_direction_sequence)
                        f.write(f"direction : {','.join(compressed_directions)}\n")
                    
                    # Write Start and Finish coordinates (adjusted to cropped map)
                    start_pos = self.canvas.find_block_position('S')
                    finish_pos = self.canvas.find_block_position('F')
                    
                    if start_pos:
                        # Adjust coordinates relative to cropped map
                        cropped_start_x = start_pos[0] - crop_min_x
                        cropped_start_y = start_pos[1] - crop_min_y
                        f.write(f"start : {cropped_start_x},{cropped_start_y}\n")
                    if finish_pos:
                        # Adjust coordinates relative to cropped map
                        cropped_finish_x = finish_pos[0] - crop_min_x
                        cropped_finish_y = finish_pos[1] - crop_min_y
                        f.write(f"finish : {cropped_finish_x},{cropped_finish_y}\n")
                    
                    # Write possible entry/exit directions
                    start_entry, finish_exit = self.canvas.get_possible_entry_exit_directions()
                    if start_entry:
                        f.write(f"Possible Start Entry : {','.join(start_entry)}\n")
                    if finish_exit:
                        f.write(f"Possible Finish Exit : {','.join(finish_exit)}\n")
                
                messagebox.showinfo("Success", f"Map saved to {filename} (auto-cropped to {crop_width}x{crop_height})")
                self.status_var.set(f"Map saved to {os.path.basename(filename)} (cropped to {crop_width}x{crop_height})")
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
                
                # Separate map data, trap data, direction data and coordinates
                map_lines = []
                trap_lines = []
                direction_line = None
                start_line = None
                finish_line = None
                start_entry_line = None
                finish_exit_line = None
                parsing_trap_layer = False
                
                for line in lines:
                    line = line.strip()
                    if line.startswith('direction :'):
                        direction_line = line
                    elif line.startswith('start :'):
                        start_line = line
                    elif line.startswith('finish :'):
                        finish_line = line
                    elif line.startswith('Possible Start Entry :'):
                        start_entry_line = line
                    elif line.startswith('Possible Finish Exit :'):
                        finish_exit_line = line
                    elif line == '# Trap Layer':
                        parsing_trap_layer = True
                    elif line and not line.startswith('#'):
                        if parsing_trap_layer:
                            trap_lines.append(line)
                        else:
                            map_lines.append(line)
                
                if not map_lines:
                    messagebox.showerror("Error", "No valid map data found in file")
                    return
                
                # Update grid dimensions based on loaded map
                height = len(map_lines)
                width = max(len(line) for line in map_lines) if map_lines else 0
                
                # Ensure even dimensions
                if width % 2 != 0:
                    width +=  1
                if height % 2 != 0:
                    height += 1
                
                # Update UI controls
                self.width_var.set(width)
                self.height_var.set(height)
                
                # Update canvas
                self.canvas.set_grid_size(width, height)
                
                # Clear the trap layer (spike_grid) when loading a new map
                self.canvas.spike_grid = [['.' for _ in range(width)] for _ in range(height)]
                
                # Clear existing asterisk data
                self.canvas.clear_asterisk_path()
                
                # Clear existing path marks
                self.canvas.start_path_marks = []
                self.canvas.finish_path_marks = []
                
                # Load the map data
                asterisk_positions = []
                for row_idx, line in enumerate(map_lines):
                    if row_idx < height:
                        for col_idx, char in enumerate(line):
                            if col_idx < width:
                                if char in ['#', 'S', 'F', '*', ',', '1', '2', '3', '4', '5', '6', '7', '8', '9']:
                                    self.canvas.grid[row_idx][col_idx] = char
                                    if char == '*':
                                        asterisk_positions.append((col_idx, row_idx))
                                else:
                                    self.canvas.grid[row_idx][col_idx] = '.'
                
                # Load trap layer data if available
                if trap_lines:
                    for row_idx, line in enumerate(trap_lines):
                        if row_idx < height:
                            for col_idx, char in enumerate(line):
                                if col_idx < width:
                                    if char in ['!', '?', 'N']:
                                        self.canvas.spike_grid[row_idx][col_idx] = char
                                    else:
                                        self.canvas.spike_grid[row_idx][col_idx] = '.'
                
                # Process direction data if available
                if direction_line and asterisk_positions:
                    # Parse direction data
                    direction_part = direction_line.split(':', 1)[1].strip()
                    directions_list = [d.strip() for d in direction_part.split(',') if d.strip()]
                    
                    # Reconstruct path from direction sequence
                    start_pos = self.canvas.find_block_position('S')
                    if start_pos and directions_list:
                        self.canvas.asterisk_direction_sequence = directions_list
                        
                        # Rebuild path by following directions (2x2 movement)
                        start_x, start_y = start_pos
                        current_x, current_y = start_x, start_y  # Top-left of Start block
                        
                        path = [(current_x, current_y)]
                        directions_at_pos = {}
                        
                        # Center of first 2x2 block
                        center_pos = (current_x + 1, current_y + 1)
                        directions_at_pos[center_pos] = []
                        
                        direction_vectors = {
                            'up': (0, -2),      # Move by 2 cells for 2x2 blocks
                            'down': (0, 2),
                            'left': (-2, 0),
                            'right': (2, 0)
                        };
                        
                        for direction in directions_list:
                            if direction in direction_vectors:
                                # Record direction at current center position
                                center_pos = (current_x + 1, current_y + 1)
                                if center_pos in directions_at_pos:
                                    directions_at_pos[center_pos].append(direction)
                                else:
                                    directions_at_pos[center_pos] = [direction]
                                
                                # Move to next 2x2 block position
                                dx, dy = direction_vectors[direction]
                                current_x += dx
                                current_y += dy
                                
                                # Add new position to path
                                if (current_x, current_y) not in path:
                                    path.append((current_x, current_y))
                                
                                # Initialize directions for new center position
                                new_center = (current_x + 1, current_y + 1)
                                if new_center not in directions_at_pos:
                                    directions_at_pos[new_center] = []
                        
                        self.canvas.asterisk_path = path
                        self.canvas.asterisk_directions = directions_at_pos
                
                # Process start and finish coordinates if provided
                if start_line:
                    try:
                        coords_part = start_line.split(':', 1)[1].strip()
                        start_x, start_y = map(int, coords_part.split(','))
                        self.status_var.set(f"Start coordinates loaded: ({start_x}, {start_y})")
                    except ValueError:
                        self.status_var.set("Warning: Invalid start coordinates format")
                
                if finish_line:
                    try:
                        coords_part = finish_line.split(':', 1)[1].strip()
                        finish_x, finish_y = map(int, coords_part.split(','))
                        self.status_var.set(f"Finish coordinates loaded: ({finish_x}, {finish_y})")
                    except ValueError:
                        self.status_var.set("Warning: Invalid finish coordinates format")
                
                # Process start and finish entry/exit directions
                if start_entry_line:
                    try:
                        directions_part = start_entry_line.split(':', 1)[1].strip()
                        entry_directions = [d.strip() for d in directions_part.split(',') if d.strip()]
                        
                        # Convert entry directions back to mark directions (opposite)
                        direction_opposites = {
                            'down': 'top',
                            'up': 'bottom', 
                            'right': 'left',
                            'left': 'right'
                        }
                        
                        self.canvas.start_path_marks = [direction_opposites.get(d, d) for d in entry_directions if d in direction_opposites]
                        print(f"DEBUG: Loaded start path marks: {self.canvas.start_path_marks}")  # Debug
                    except Exception as e:
                        print(f"Error parsing start entry directions: {e}")
                
                if finish_exit_line:
                    try:
                        directions_part = finish_exit_line.split(':', 1)[1].strip()
                        exit_directions = [d.strip() for d in directions_part.split(',') if d.strip()]
                        
                        # Convert exit directions back to mark directions (opposite)
                        direction_opposites = {
                            'down': 'top',
                            'up': 'bottom', 
                            'right': 'left',
                            'left': 'right'
                        }
                        
                        self.canvas.finish_path_marks = [direction_opposites.get(d, d) for d in exit_directions if d in direction_opposites]
                        print(f"DEBUG: Loaded finish path marks: {self.canvas.finish_path_marks}")  # Debug
                    except Exception as e:
                        print(f"Error parsing finish exit directions: {e}")
                
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
        self.root.bind("<KeyPress-q>", lambda e: self.set_drawing_mode('start_path'))
        self.root.bind("<KeyPress-Q>", lambda e: self.set_drawing_mode('start_path'))
        self.root.bind("<KeyPress-t>", lambda e: self.set_drawing_mode('finish_path'))
        self.root.bind("<KeyPress-T>", lambda e: self.set_drawing_mode('finish_path'))
        self.root.bind("<KeyPress-e>", lambda e: self.set_drawing_mode('erase'))
        self.root.bind("<KeyPress-E>", lambda e: self.set_drawing_mode('erase'))
        self.root.bind("<KeyPress-r>", lambda e: self.set_drawing_mode('select'))
        self.root.bind("<KeyPress-R>", lambda e: self.set_drawing_mode('select'))
        self.root.bind("<KeyPress-1>", lambda e: self.set_drawing_mode('spike1'))
        self.root.bind("<KeyPress-2>", lambda e: self.set_drawing_mode('spike2'))
        self.root.bind("<KeyPress-n>", lambda e: self.set_drawing_mode('cannon'))
        self.root.bind("<KeyPress-N>", lambda e: self.set_drawing_mode('cannon'))
        self.root.bind("<KeyPress-p>", lambda e: self.set_drawing_mode('portal'))
        self.root.bind("<KeyPress-P>", lambda e: self.set_drawing_mode('portal'))
        self.root.bind("<KeyPress-i>", lambda e: self.set_drawing_mode('fish'))
        self.root.bind("<KeyPress-I>", lambda e: self.set_drawing_mode('fish'))
        
        # Undo/Redo shortcuts
        self.root.bind("<Control-z>", lambda e: self.undo())
        self.root.bind("<Control-Z>", lambda e: self.undo())
        self.root.bind("<Control-y>", lambda e: self.redo())
        self.root.bind("<Control-Y>", lambda e: self.redo())
        self.root.bind("<Control-Shift-Z>", lambda e: self.redo())  # Alternative redo
        
        # Zoom shortcuts (Aseprite-style) - using + and - keys, numpad keys for zoom levels
        self.root.bind("<plus>", lambda e: self.zoom_in())
        self.root.bind("<equal>", lambda e: self.zoom_in())  # + key without shift
        self.root.bind("<KP_Add>", lambda e: self.zoom_in())  # Numpad +
        self.root.bind("<minus>", lambda e: self.zoom_out())
        self.root.bind("<KP_Subtract>", lambda e: self.zoom_out())  # Numpad -
        # Note: 1 and 2 keys reserved for spike tools
        self.root.bind("<KP_1>", lambda e: self.zoom_reset())  # Numpad 1 for 100%
        self.root.bind("<KP_2>", lambda e: self.zoom_to_level(200))  # Numpad 2 for 200%
        self.root.bind("<Key-3>", lambda e: self.zoom_to_level(300))  # 3 key for 300%
        self.root.bind("<Key-4>", lambda e: self.zoom_to_level(400))  # 4 key for 400%
        
        # Mouse wheel zoom (when over canvas)
        self.canvas.bind("<Button-4>", self._on_mouse_wheel_up)  # Linux
        self.canvas.bind("<Button-5>", self._on_mouse_wheel_down)  # Linux
        self.canvas.bind("<MouseWheel>", self._on_mouse_wheel)  # Windows/Mac
        
        # Selection shortcuts
        self.root.bind("<Control-c>", lambda e: self.copy_selection())
        self.root.bind("<Control-C>", lambda e: self.copy_selection())
        self.root.bind("<Control-v>", lambda e: self.paste_selection())
        self.root.bind("<Control-V>", lambda e: self.paste_selection())
        self.root.bind("<Delete>", lambda e: self.delete_selection())
        self.root.bind("<Escape>", lambda e: self.clear_selection_ui())
        
        # Make sure the root window can receive keyboard focus
        self.root.focus_set()
    
    def copy_selection(self):
        """Copy the selected area to clipboard"""
        self.canvas.copy_selection()
        self.update_selection_buttons()
    
    def paste_selection(self):
        """Paste from clipboard to current selection"""
        # If there's a current selection, use its top-left corner as paste position
        bounds = self.canvas.get_selection_bounds()
        if bounds:
            min_x, min_y, max_x, max_y = bounds
            paste_x, paste_y = min_x, min_y
        else:
            # If no selection, paste at (0, 0)
            paste_x, paste_y = 0, 0
        
        self.canvas.paste_selection(paste_x, paste_y)
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
    
    def _on_mouse_wheel(self, event):
        """Handle mouse wheel zoom and scrolling (Windows/Mac)"""
        if event.state & 0x4:  # Ctrl pressed - vertical scrolling
            if event.delta > 0:
                self.canvas.yview_scroll(-1, "units")
            else:
                self.canvas.yview_scroll(1, "units")
        elif event.state & 0x1:  # Shift pressed - horizontal scrolling
            if event.delta > 0:
                self.canvas.xview_scroll(-1, "units")
            else:
                self.canvas.xview_scroll(1, "units")
        else:  # No modifier - zoom
            if event.delta > 0:
                self.canvas.zoom_in(event.x, event.y)
            else:
                self.canvas.zoom_out(event.x, event.y)
    
    def _on_mouse_wheel_up(self, event):
        """Handle mouse wheel up (Linux)"""
        if event.state & 0x4:  # Ctrl pressed - vertical scrolling
            self.canvas.yview_scroll(-1, "units")
        elif event.state & 0x1:  # Shift pressed - horizontal scrolling
            self.canvas.xview_scroll(-1, "units")
        else:  # No modifier - zoom
            self.canvas.zoom_in(event.x, event.y)
    
    def _on_mouse_wheel_down(self, event):
        """Handle mouse wheel down (Linux)"""
        if event.state & 0x4:  # Ctrl pressed - vertical scrolling
            self.canvas.yview_scroll(1, "units")
        elif event.state & 0x1:  # Shift pressed - horizontal scrolling
            self.canvas.xview_scroll(1, "units")
        else:  # No modifier - zoom
            self.canvas.zoom_out(event.x, event.y)

    def run(self):
        self.root.mainloop()

if __name__ == "__main__":
    app = MapDrawer()
    app.run()


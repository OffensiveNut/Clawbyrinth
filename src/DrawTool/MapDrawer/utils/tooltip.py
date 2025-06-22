"""
ToolTip utility for MapDrawer
Provides hover tooltips for UI elements
"""

import tkinter as tk


class ToolTip:
    """
    Creates a tooltip for a given tkinter widget.
    """
    
    def __init__(self, widget, text='widget info'):
        self.widget = widget
        self.text = text
        self.tooltip_window = None
        self.widget.bind("<Enter>", self.on_enter)
        self.widget.bind("<Leave>", self.on_leave)

    def on_enter(self, event=None):
        """Show tooltip when mouse enters widget"""
        self.show_tooltip()

    def on_leave(self, event=None):
        """Hide tooltip when mouse leaves widget"""
        self.hide_tooltip()

    def show_tooltip(self):
        """Display the tooltip"""
        if self.tooltip_window or not self.text:
            return
            
        x, y, cx, cy = self.widget.bbox("insert")
        x += self.widget.winfo_rootx() + 25
        y += self.widget.winfo_rooty() + 20
        
        # Create tooltip window
        self.tooltip_window = tw = tk.Toplevel(self.widget)
        tw.wm_overrideredirect(True)
        tw.wm_geometry(f"+{x}+{y}")
        
        label = tk.Label(
            tw, 
            text=self.text, 
            justify='left',
            background="#ffffe0", 
            relief='solid', 
            borderwidth=1,
            font=("tahoma", "8", "normal")
        )
        label.pack(ipadx=1)

    def hide_tooltip(self):
        """Hide the tooltip"""
        tw = self.tooltip_window
        self.tooltip_window = None
        if tw:
            tw.destroy()

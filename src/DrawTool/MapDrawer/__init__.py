"""
MapDrawer - Enhanced 2D Grid Map Editor
A comprehensive Tkinter-based map editor for game design with advanced features.
"""

from .ui.main_window import MapDrawer

__version__ = "1.0.0"
__author__ = "MapDrawer Team"

# Main entry point
def run():
    """Launch the MapDrawer application"""
    app = MapDrawer()
    app.run()

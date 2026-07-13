# Created and Coded by https://www.overtake.gg/members/stormix43.412027/
import ac
import acsys
import os
import sys
import platform
import configparser
import math

if platform.architecture()[0] == "64bit":
    libdir = 'third_party/lib64'
else:
    libdir = 'third_party/lib'
sys.path.insert(0, os.path.join(os.path.dirname(__file__), libdir))
os.environ['PATH'] = os.environ['PATH'] + ";."

import ctypes
from ctypes.wintypes import MAX_PATH
from third_party.sim_info import info

app_name = "WMPS3 HUD"
app_path = "apps/python/WMPS3 HUD/"
img_path = app_path + "/img/"

# Assume config doesn't need to be updated
update_config = False

# Set config file path
config_path = app_path + "config.ini"

# Initialize configparser
config = configparser.ConfigParser()
config.read(config_path)

# Check if config file has appropriate content
if config.has_section("WMPS3 HUD") != True:
    config.add_section("WMPS3 HUD")
    update_config = True

# Load the integer value or set it to default
def get_int(config, key, default):
    global update_config
    try:
        return config.getint("WMPS3 HUD", key)
    except:
        config.set("WMPS3 HUD", key, str(default))
        update_config = True
        return default
    
# Load the boolean value or set it to default
def get_boolean(config, key, default):
    global update_config
    try:
        return config.getboolean("WMPS3 HUD", key)
    except:
        config.set("WMPS3 HUD", key, str(default))
        update_config = True
        return default  
    
# Load the float value or set it to default
def get_float(config, key, default):
    global update_config
    try:
        return config.getfloat("WMPS3 HUD", key)
    except:
        config.set("WMPS3 HUD", key, str(default))
        update_config = True
        return default
    
def init_ac_folder_in_user():
    global ac_user_folder

    dll = ctypes.windll.shell32
    buf = ctypes.create_unicode_buffer(MAX_PATH + 1)

    if dll.SHGetSpecialFolderPathW(None, buf, 0x0005, False):
        document_folder = buf.value
        ac_folder = os.path.join(document_folder, 'Assetto Corsa')

        if os.path.isdir(ac_folder):
            ac_user_folder = ac_folder

def get_speed_unit():
    init_ac_folder_in_user()

    path = os.path.join(ac_user_folder, "cfg/gameplay.ini")
    gameplay_config = configparser.ConfigParser()
    gameplay_config.read(path)

    try:
        use_kmh = not(bool(int(gameplay_config.get("OPTIONS", "USE_MPH"))))
        return use_kmh
    except:
        ac.console("Error loading Speed Unit.")
        ac.console("Using default values.")
        use_kmh = True
        return use_kmh

# Default Apps Positions
default_config_position = (50, 420)
default_tachometer_position = (3306, 1453)
default_digital_dash_position = (2837, 1835)
default_turbo_gauge_position = (3190, 1053)
default_map_position = (96 , 771)
default_position_player_overlay = (57, 33)
default_position_rivals_overlay = (1310, 33)
default_rear_view_mirror_overlay = (880 , 59)
default_time_position = (1065, 75)

# Apps Scales
config_scale = get_float(config, "config_scale", 100.0) / 100
tachometer_scale = get_float(config, "tachometer_scale", 100.0) / 100
digital_dash_scale = get_float(config, "digital_dash_scale", 100.0) / 100
turbo_gauge_scale = get_float(config, "turbo_gauge_scale", 100.0) / 100
map_overlay_scale = get_float(config, "map_overlay_scale", 100.0) / 100
player_rivals_overlays_scale = get_float(config, "player_rivals_overlays_scale", 100.0) / 100
rear_view_mirror_overlay_scale = get_float(config, "rear_view_mirror_overlay_scale", 100.0) / 100
time_scale = get_float(config, "time_scale", 100.0) / 100

# Player/Rivals Icons
player_icon_number = get_int(config, "player_icon_number", 0)
rival_1st_icon_number = get_int(config, "rival_1st_icon_number", 0)
rival_2nd_icon_number = get_int(config, "rival_2nd_icon_number", 0)
rival_3rd_icon_number = get_int(config, "rival_3rd_icon_number", 0)

# Color Presets
color_presets = [
    (1, 1, 1, 1),
    (1, 0, 0, 1),
    (1, 0.5, 0, 1),
    (1, 0.3, 0, 1),
    (1, 0.6, 0, 1),
    (1, 0.8, 0, 1),
    (1, 1, 0, 1),
    (1, 1, 0.5, 1),
    (0.8, 1, 0.2, 1),
    (0.5, 1, 0, 1),
    (0, 1, 0, 1),
    (0, 0.8, 0.2, 1),
    (0, 0.8, 0.6, 1),
    (0, 0.8, 1, 1),
    (0, 0.5, 1, 1),
    (0, 0, 1, 1),
    (0.3, 0, 1, 1),
    (0.5, 0, 1, 1),
    (0.7, 0, 1, 1),
    (1, 0, 1, 1),
    (1, 0, 0.8, 1),
    (0, 0, 0, 1),
    (1, 1, 1, 0.66),
    (1, 0, 0, 0.66),
    (1, 0.5, 0, 0.66),
    (1, 0.3, 0, 0.66),
    (1, 0.6, 0, 0.66),
    (1, 0.8, 0, 0.66),
    (1, 1, 0, 0.66),
    (1, 1, 0.5, 0.66),
    (0.8, 1, 0.2, 0.66),
    (0.5, 1, 0, 0.66),
    (0, 1, 0, 0.66),
    (0, 0.8, 0.2, 0.66),
    (0, 0.8, 0.6, 0.66),
    (0, 0.8, 1, 0.66),
    (0, 0.5, 1, 0.66),
    (0, 0, 1, 0.66),
    (0.3, 0, 1, 0.66),
    (0.5, 0, 1, 0.66),
    (0.7, 0, 1, 0.66),
    (1, 0, 1, 0.66),
    (1, 0, 0.8, 0.66),
    (0, 0, 0, 0.66),
    (1, 1, 1, 0.33),
    (1, 0, 0, 0.33),
    (1, 0.5, 0, 0.33),
    (1, 0.3, 0, 0.33),
    (1, 0.6, 0, 0.33),
    (1, 0.8, 0, 0.33),
    (1, 1, 0, 0.33),
    (1, 1, 0.5, 0.33),
    (0.8, 1, 0.2, 0.33),
    (0.5, 1, 0, 0.33),
    (0, 1, 0, 0.33),
    (0, 0.8, 0.2, 0.33),
    (0, 0.8, 0.6, 0.33),
    (0, 0.8, 1, 0.33),
    (0, 0.5, 1, 0.33),
    (0, 0, 1, 0.33),
    (0.3, 0, 1, 0.33),
    (0.5, 0, 1, 0.33),
    (0.7, 0, 1, 0.33),
    (1, 0, 1, 0.33),
    (1, 0, 0.8, 0.33),
    (0, 0, 0, 0.33)
]

# Player/Rivals Background Color
player_overlay_background_color_index = get_int(config, "player_overlay_background_color_index", 0)
rival_1st_overlay_background_color_index = get_int(config, "rival_1st_overlay_background_color_index", 0)
rival_2nd_overlay_background_color_index = get_int(config, "rival_2nd_overlay_background_color_index", 0)
rival_3rd_overlay_background_color_index = get_int(config, "rival_3rd_overlay_background_color_index", 0)

# Player/Rivals Types
player_type = get_int(config, "player_type", 0)
rival_1st_type = get_int(config, "rival_1st_type", 0)
rival_2nd_type = get_int(config, "rival_2nd_type", 0)
rival_3rd_type = get_int(config, "rival_3rd_type", 0)

# Apps Opacity
tachometer_background_opacity_index = get_int(config, "tachometer_background_opacity_index", 0)
turbo_gauge_background_opacity_index = get_int(config, "turbo_gauge_background_opacity_index", 0)

# Opacity Presets
opacity_presets = [
    (1.0, 1.0, 1.0, 0.0),
    (1.0, 1.0, 1.0, 0.05),
    (1.0, 1.0, 1.0, 0.1),
    (1.0, 1.0, 1.0, 0.15),
    (1.0, 1.0, 1.0, 0.2),
    (1.0, 1.0, 1.0, 0.25),
    (1.0, 1.0, 1.0, 0.3),
    (1.0, 1.0, 1.0, 0.35),
    (1.0, 1.0, 1.0, 0.4),
    (1.0, 1.0, 1.0, 0.45),
    (1.0, 1.0, 1.0, 0.5),
    (1.0, 1.0, 1.0, 0.55),
    (1.0, 1.0, 1.0, 0.6),
    (1.0, 1.0, 1.0, 0.65),
    (1.0, 1.0, 1.0, 0.7),
    (1.0, 1.0, 1.0, 0.75),
    (1.0, 1.0, 1.0, 0.8),
    (1.0, 1.0, 1.0, 0.85),
    (1.0, 1.0, 1.0, 0.9),
    (1.0, 1.0, 1.0, 0.95),
    (1.0, 1.0, 1.0, 1.0),
]

# Extra Options
player_rivals_type_position_index = get_int(config, "player_rivals_type_position_index", 0)
player_mode_type = get_int(config, "player_mode_type", 0)
mode_type_position_index = get_int(config, "mode_type_position_index", 0)
show_player_rivals_current_positions = get_boolean(config, "show_player_rivals_current_positions", True)
show_map_overlay_text = get_boolean(config, "show_map_overlay_text", True)

maxRpm_state = [3949, 4099, 4949, 5099, 5949, 6099, 6949, 7099, 7949, 8099, 8949, 9099, 9949, 10099, 10949, 11099, 11949, 12099, 13099, 14099, 15099, 16099, 18099]
redRpm_state = [12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 12000, 16000, 16000, 16000, 16000, 20000, 20000]

# Player Type Position and Scale Presets
configurations_player_type_presets = {
    0: {'position_x': 227, 'position_y': 26, 'scale_x': 300, 'scale_y': 53},
    1: {'position_x': 193, 'position_y': 33, 'scale_x': 154, 'scale_y': 28},
    2: {'position_x': 193, 'position_y': 58, 'scale_x': 148, 'scale_y': 26},
}
# Rivals Type Position and Scale Presets
configurations_rival_type_presets = {
    0: {'position_x': 32, 'position_y': 26, 'scale_x': 300, 'scale_y': 53},
    1: {'position_x': 208, 'position_y': 33, 'scale_x': 154, 'scale_y': 28},
    2: {'position_x': 210, 'position_y': 58, 'scale_x': 148, 'scale_y': 26},
}

# Player Mode Type Position and Scale Presets
configurations_player_mode_type = {
    0: {'position_x': 228, 'position_y': 31, 'scale_x': 296, 'scale_y': 37.5},
    1: {'position_x': 215.5, 'position_y': 28, 'scale_x': 200, 'scale_y': 25},
    2: {'position_x': 42, 'position_y': 120, 'scale_x': 120, 'scale_y': 15},
}

# Update config if necessary
if update_config:
    with open(config_path, 'w') as file_config:
        config.write(file_config)

# Initialize
timer = 0
timer2 = 0
current_car = 0
status = 0
session_type = 0
maxRpm = 1
state = 0
rpm = 1
gear = 0
gear_animation = 0
gear_animation_speed = 1
gear_current = 0
gear_delay = 0
gear_shift = False
speed = 0
speed_list = []
boost = 0
auto_on = False
handbrake_on = False
player_rivals_overlay_current = 0
rivals_1st_overlay = 0
rivals_2nd_overlay = 0
rivals_3rd_overlay = 0
rivals_overlay_list = [[] for _ in range(3)]
rival_icon_data = ()
distance = 0
distance_xxdotx_list = []
distance_xdotxx_list = []
time_current = 0

# Config Abjustments
config_window_visibility = 0

class ExtGL:
    CULL_MODE_FRONT = 0
    CULL_MODE_BACK = 1
    CULL_MODE_NONE = 2
    CULL_MODE_WIREFRAME = 4
    CULL_MODE_WIREFRAME_SMOOTH = 7
    BLEND_MODE_OPAQUE = 0
    BLEND_MODE_ALPHA_BLEND = 1
    BLEND_MODE_ALPHA_TEST = 2
    BLEND_MODE_ALPHA_SUBSTRACT = 3
    BLEND_MODE_ALPHA_ADD = 4
    BLEND_MODE_MULTIPLY_BOTH = 5
    BLEND_MODE_ALPHA_MAX = 13

def acMain(ac_version):
    global config_window, config_scale, config_scale_spinner
    global tachometer_window, digital_dash_window, turbo_gauge_window, map_overlay_window, player_overlay_window, rivals_overlay_window, rear_view_mirror_overlay_window, time_counter_window
    global tachometer_scale, tachometer_scale_spinner, tachometer_background_opacity_index, tachometer_background_opacity_spinner, tachometer_background, tachometer_carbon, tachometer_rpm, tachometer_needle, tachometer_dot
    global digital_dash_scale, digital_dash_scale_spinner, digital_dash_background, gear, digital_gears, speed, digital_speed_numbers, speed_uom, uom_kmh, auto_manual, handbrake
    global turbo_gauge_scale, turbo_gauge_scale_spinner, turbo_gauge_background_opacity_index, turbo_gauge_background_opacity_spinner, turbo_gauge_background, turbo_gauge_carbon, turbo_gauge_unit, turbo_gauge_needle, turbo_gauge_dot
    global map_overlay_scale, map_overlay_scale_spinner, track_length, map_overlay_background, map_numbers, map_dot, map_stuff
    global player_rivals_overlays_scale, player_rivals_overlays_scale_spinner, position_overlay_numbers, position_overlay_player_background, position_overlay_rivals_background, rivals_overlay_list
    global rear_view_mirror_overlay_scale, rear_view_mirror_overlay_scale_spinner, rear_view_mirror_overlay_background
    global time_current, time_numbers, time_bg, time_scale_spinner
    global cars_in_session
    global resolution_presets_label, resolution_720p_button, resolution_1080p_button, resolution_1080p_uw_button, resolution_1440p_button, resolution_1440p_uw_button, resolution_4k_button
    global scales_label, config_scale_label, tachometer_scale_label, digital_dash_scale_label, turbo_gauge_scale_label, map_overlay_scale_label, rear_view_mirror_overlay_scale_label, player_rivals_overlays_scale_label, time_scale_label
    global player_rivals_customization_label, player_label, rival_1_label, rival_2_label, rival_3_label, icon_label, color_label, type_label
    global player_icon_selected, change_player_icon_spinner, player_icon_number, change_rival_1st_icon_spinner, change_rival_2nd_icon_spinner, change_rival_3rd_icon_spinner
    global player_overlay_background_color_spinner, rival_1st_overlay_background_color_spinner, rival_2nd_overlay_background_color_spinner, rival_3rd_overlay_background_color_spinner
    global rival_text_selected, change_player_type_spinner, change_rival_1st_type_spinner, change_rival_2nd_type_spinner, change_rival_3rd_type_spinner, player_rivals_type_position_spinner
    global opacity_label, tachometer_background_opacity_label, turbo_background_opacity_label
    global player_rivals_preset_label, player_mode_type_label, player_mode_position_label, show_player_rivals_current_position_label, show_map_overlay_text_label
    global player_mode_type_selected, player_mode_type_spinner, player_mode_position_spinner
    global show_player_rivals_current_positions, show_map_overlay_text
    global version_label, developed_label, acxwmps3_logo
    global reset_all_button

    uom_kmh = get_speed_unit()

    cars_in_session = ac.getCarsCount()

    track_length = ac.getTrackLength(0)

    # Tachometer Window
    tachometer_window = ac.newApp("WMPS3 HUD Tachometer")
    ac.setTitle(tachometer_window, "")
    ac.drawBorder(tachometer_window, 0)
    ac.setIconPosition(tachometer_window, 0, -10000)
    ac.setSize(tachometer_window, 500 * tachometer_scale, 500 * tachometer_scale)

    tachometer_background = ac.newTexture(img_path + "tachometer/tachometer_background.png")

    tachometer_carbon = ac.newTexture(img_path + "tachometer/tachometer_carbon.png")

    tachometer_rpm = []
    for i in ["3500", "4000", "4500", "5000", "5500", "6000", "6500", "7000", "7500", "8000", "8500", "9000", "9500", "10000", "10500", "11000", "11500", "12000", "13000", "14000", "15000", "16000", "18000"]:
        tachometer_rpm.append(ac.newTexture(img_path + "tachometer_rpm/rpm_" + i + ".png"))

    tachometer_needle = ac.newTexture(img_path + "tachometer/tachometer_needle.png")

    tachometer_dot = ac.newTexture(img_path + "tachometer/tachometer_dot.png")

    # Digital Dash Window
    digital_dash_window = ac.newApp("WMPS3 HUD Digital Dash")
    ac.setTitle(digital_dash_window, "")
    ac.drawBorder(digital_dash_window, 0)
    ac.setIconPosition(digital_dash_window, 0, -10000)
    ac.setSize(digital_dash_window, 864 * digital_dash_scale, 247 * digital_dash_scale)

    digital_dash_background = ac.newTexture(img_path + "digital_dash/digital_dash_background.png")

    digital_gears = ac.newTexture(img_path + "digital_dash/digital_gears.png")

    digital_speed_numbers = ac.newTexture(img_path + "digital_dash/digital_speed_numbers.png")

    speed_uom = ac.newTexture(img_path + "digital_dash/speed_uom.png")

    auto_manual = ac.newTexture(img_path + "digital_dash/auto_manual.png")
    
    handbrake = ac.newTexture(img_path + "digital_dash/handbrake.png")

    # Turbo Gauge Window
    turbo_gauge_window = ac.newApp("WMPS3 HUD Turbo Gauge")
    ac.setTitle(turbo_gauge_window, "")
    ac.drawBorder(turbo_gauge_window, 0)
    ac.setIconPosition(turbo_gauge_window, 0, -10000)
    ac.setSize(turbo_gauge_window, 300 * turbo_gauge_scale, 300 * turbo_gauge_scale)

    turbo_gauge_background = ac.newTexture(img_path + "turbo_gauge/turbo_gauge_background.png")

    turbo_gauge_carbon = ac.newTexture(img_path + "turbo_gauge/turbo_gauge_carbon.png")

    turbo_gauge_unit = ac.newTexture(img_path + "turbo_gauge/turbo_gauge_unit_x100kpa.png")

    turbo_gauge_needle = ac.newTexture(img_path + "turbo_gauge/turbo_gauge_needle.png")

    turbo_gauge_dot = ac.newTexture(img_path + "turbo_gauge/turbo_gauge_dot.png")

    # Map Overlay Window
    map_overlay_window = ac.newApp("WMPS3 HUD Map Overlay")
    ac.setTitle(map_overlay_window, "")
    ac.drawBorder(map_overlay_window, 0)
    ac.setIconPosition(map_overlay_window, 0, -10000)
    ac.setSize(map_overlay_window, 267 * map_overlay_scale, 267 * map_overlay_scale)

    map_overlay_background = ac.newTexture(img_path + "map_overlay/map.png")

    map_numbers = ac.newTexture(img_path + "map_overlay/map_numbers.png")

    map_dot = ac.newTexture(img_path + "map_overlay/map_dot.png")

    map_stuff = ac.newTexture(img_path + "map_overlay/map_stuff.png")

    # Position Overlay Player Window
    player_overlay_window = ac.newApp("WMPS3 HUD Position Overlay Player")
    ac.setTitle(player_overlay_window, "")
    ac.drawBorder(player_overlay_window, 0)
    ac.setIconPosition(player_overlay_window, 0, -10000)
    ac.setSize(player_overlay_window, 560 * player_rivals_overlays_scale, 120 * player_rivals_overlays_scale)

    position_overlay_player_background = ac.newTexture(img_path + "position_overlays/position_overlay_player_background.png")

    position_overlay_numbers = ac.newTexture(img_path + "position_overlays/position_overlay_numbers.png")

    # Test Character Icon Spinner
    player_icon_selected = []
    for i in range(0, 62):
        player_icon_selected.append(ac.newTexture(img_path + "icons/icon_" + str(i) + ".png"))

    # Initialize rivals_overlay_list
    rivals_overlay_list = [[] for _ in range(3)]

    # Test Rival Text
    rival_text_selected = []
    for i in range(0, 16):
        rival_text_selected.append(ac.newTexture(img_path + "etc/overlay_text/overlay_text_" + str(i) + ".png"))

    # Mode Type
    player_mode_type_selected = []
    for i in range(0, 21):
        player_mode_type_selected.append(ac.newTexture(img_path + "etc/player_mode_type/player_mode_type_" + str(i) + ".png"))

    # Position Overlay Rivals Window
    rivals_overlay_window = ac.newApp("WMPS3 HUD Position Overlay Rivals")
    ac.setTitle(rivals_overlay_window, "")
    ac.drawBorder(rivals_overlay_window, 0)
    ac.setIconPosition(rivals_overlay_window, 0, -10000)
    ac.setSize(rivals_overlay_window, 560 * player_rivals_overlays_scale, 120 * player_rivals_overlays_scale)

    position_overlay_rivals_background = ac.newTexture(img_path + "position_overlays/position_overlay_rivals_background.png")

    # Rear View Mirror Overlay Window
    rear_view_mirror_overlay_window = ac.newApp("WMPS3 HUD Rear View Mirror Overlay")
    ac.setTitle(rear_view_mirror_overlay_window, "")
    ac.drawBorder(rear_view_mirror_overlay_window, 0)
    ac.setIconPosition(rear_view_mirror_overlay_window, 0, -10000)
    ac.setSize(rear_view_mirror_overlay_window, 700 * rear_view_mirror_overlay_scale, 210 * rear_view_mirror_overlay_scale)

    rear_view_mirror_overlay_background = ac.newTexture(img_path + "etc/rear_view_mirror_overlay.png")

    # Time Window
    time_counter_window = ac.newApp("WMPS3 HUD Time Counter")
    ac.setTitle(time_counter_window, "")
    ac.drawBorder(time_counter_window, 0)
    ac.setIconPosition(time_counter_window, 0, -10000)
    ac.setSize(time_counter_window, 428 * time_scale, 100 * time_scale)

    time_bg = ac.newTexture(img_path + "time/time_bg.png")

    time_numbers = ac.newTexture(img_path + "time/time_numbers.png")

    # Config Window
    config_window = ac.newApp("WMPS3 HUD Config")
    ac.setTitle(config_window, "")
    ac.drawBorder(config_window, 0)
    ac.setSize(config_window, 1000 * config_scale, 840 * config_scale)
    ac.setVisible(config_window, config_window_visibility)
    ac.setFontSize(config_window, 100 * config_scale)
    ac.addOnAppActivatedListener(config_window, config_window_activated)
    ac.addOnAppDismissedListener(config_window, config_window_deactivated)

    acxwmps3_logo = ac.newTexture(img_path + "etc/acxwmps3_logo.png")

    # Version Label
    version_label = ac.addLabel(config_window, "1.0.0")
    ac.setPosition(version_label, 897 * config_scale, 26 * config_scale)
    ac.setFontSize(version_label, 40 * config_scale)
    ac.setCustomFont(version_label, "Strait", 0, 1)
    ac.setFontColor(version_label, 1, 1, 1, 1)

    # Developed Label
    developed_label = ac.addLabel(config_window, "Developed and Designed by StoRMiX43")
    ac.setPosition(developed_label, 200 * config_scale, 790 * config_scale)
    ac.setFontSize(developed_label, 36.2 * config_scale)
    ac.setCustomFont(developed_label, "Strait", 0, 1)
    ac.setFontColor(developed_label, 1, 1, 1, 1)

    # Resolution Presets Label
    resolution_presets_label = ac.addLabel(config_window, "RESOLUTION PRESETS")
    ac.setPosition(resolution_presets_label, 334 * config_scale, 105 * config_scale)
    ac.setFontSize(resolution_presets_label, 37 * config_scale)
    ac.setCustomFont(resolution_presets_label, "Strait", 0, 1)
    ac.setFontColor(resolution_presets_label, 1, 1, 1, 1)

    resolution_720p_button = ac.addButton(config_window, "720P")
    ac.setPosition(resolution_720p_button, 28 * config_scale, 155 * config_scale)
    ac.setSize(resolution_720p_button, 147 * config_scale, 40 * config_scale)
    ac.setFontSize(resolution_720p_button, 34 * config_scale)
    ac.setCustomFont(resolution_720p_button, "Strait", 0, 0)
    ac.setFontColor(resolution_720p_button, 1, 1, 1, 1)
    ac.addOnClickedListener(resolution_720p_button, resolution_720p_button_clicked)

    resolution_1080p_button = ac.addButton(config_window, "1080P")
    ac.setPosition(resolution_1080p_button, 188 * config_scale, 155 * config_scale)
    ac.setSize(resolution_1080p_button, 147 * config_scale, 40 * config_scale)
    ac.setFontSize(resolution_1080p_button, 34 * config_scale)
    ac.setCustomFont(resolution_1080p_button, "Strait", 0, 0)
    ac.setFontColor(resolution_1080p_button, 1, 1, 1, 1)
    ac.addOnClickedListener(resolution_1080p_button, resolution_1080p_button_clicked)

    resolution_1080p_uw_button = ac.addButton(config_window, "1080P UW")
    ac.setPosition(resolution_1080p_uw_button, 348 * config_scale, 155 * config_scale)
    ac.setSize(resolution_1080p_uw_button, 147 * config_scale, 40 * config_scale)
    ac.setFontSize(resolution_1080p_uw_button, 34 * config_scale)
    ac.setCustomFont(resolution_1080p_uw_button, "Strait", 0, 0)
    ac.setFontColor(resolution_1080p_uw_button, 1, 1, 1, 1)
    ac.addOnClickedListener(resolution_1080p_uw_button, resolution_1080p_uw_button_clicked)

    resolution_1440p_button = ac.addButton(config_window, "1440P")
    ac.setPosition(resolution_1440p_button, 508 * config_scale, 155 * config_scale)
    ac.setSize(resolution_1440p_button, 147 * config_scale, 40 * config_scale)
    ac.setFontSize(resolution_1440p_button, 34 * config_scale)
    ac.setCustomFont(resolution_1440p_button, "Strait", 0, 0)
    ac.setFontColor(resolution_1440p_button, 1, 1, 1, 1)
    ac.addOnClickedListener(resolution_1440p_button, resolution_1440p_button_clicked)

    resolution_1440p_uw_button = ac.addButton(config_window, "1440P UW")
    ac.setPosition(resolution_1440p_uw_button, 668 * config_scale, 155 * config_scale)
    ac.setSize(resolution_1440p_uw_button, 147 * config_scale, 40 * config_scale)
    ac.setFontSize(resolution_1440p_uw_button, 34 * config_scale)
    ac.setCustomFont(resolution_1440p_uw_button, "Strait", 0, 0)
    ac.setFontColor(resolution_1440p_uw_button, 1, 1, 1, 1)
    ac.addOnClickedListener(resolution_1440p_uw_button, resolution_1440p_uw_button_clicked)

    resolution_4k_button = ac.addButton(config_window, "4K")
    ac.setPosition(resolution_4k_button, 828 * config_scale, 155 * config_scale)
    ac.setSize(resolution_4k_button, 147 * config_scale, 40 * config_scale)
    ac.setFontSize(resolution_4k_button, 34 * config_scale)
    ac.setCustomFont(resolution_4k_button, "Strait", 0, 0)
    ac.setFontColor(resolution_4k_button, 1, 1, 1, 1)
    ac.addOnClickedListener(resolution_4k_button, resolution_4k_button_clicked)

    # Scales Label
    scales_label = ac.addLabel(config_window, "Scales")
    ac.setPosition(scales_label, 20 * config_scale, 203 * config_scale)
    ac.setFontSize(scales_label, 37 * config_scale)
    ac.setCustomFont(scales_label, "Strait", 0, 1)
    ac.setFontColor(scales_label, 1, 1, 1, 1)

    # Config Scale Label
    config_scale_label = ac.addLabel(config_window, "Config Scale (Restart)")
    ac.setPosition(config_scale_label, 19 * config_scale, 256 * config_scale)
    ac.setFontSize(config_scale_label, 25 * config_scale)
    ac.setCustomFont(config_scale_label, "Strait", 0, 0)
    ac.setFontColor(config_scale_label, 1, 1, 1, 1)

    # Config Scale Spinner
    config_scale_spinner = ac.addSpinner(config_window, "")
    ac.setRange(config_scale_spinner, 30, 150)
    ac.setStep(config_scale_spinner, 5)
    ac.setValue(config_scale_spinner, config_scale * 100)
    ac.setPosition(config_scale_spinner, 350 * config_scale, 257 * config_scale)
    ac.setSize(config_scale_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(config_scale_spinner, 22 * config_scale)
    ac.setCustomFont(config_scale_spinner, "Strait", 0,0)
    ac.setFontColor(config_scale_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(config_scale_spinner, config_scale_spinner_clicked)

    # Tachometer Scale Label
    tachometer_scale_label = ac.addLabel(config_window, "Tachometer Scale")
    ac.setPosition(tachometer_scale_label, 19 * config_scale, 296 * config_scale)
    ac.setFontSize(tachometer_scale_label, 25 * config_scale)
    ac.setCustomFont(tachometer_scale_label, "Strait", 0, 0)
    ac.setFontColor(tachometer_scale_label, 1, 1, 1, 1)

    # Tachometer Scale Spinner
    tachometer_scale_spinner = ac.addSpinner(config_window, "")
    ac.setRange(tachometer_scale_spinner, 30, 200)
    ac.setStep(tachometer_scale_spinner, 1)
    ac.setValue(tachometer_scale_spinner, tachometer_scale * 100)
    ac.setPosition(tachometer_scale_spinner, 350 * config_scale, 297 * config_scale)
    ac.setSize(tachometer_scale_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(tachometer_scale_spinner, 22 * config_scale)
    ac.setCustomFont(tachometer_scale_spinner, "Strait", 0, 0)
    ac.setFontColor(tachometer_scale_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(tachometer_scale_spinner, tachometer_scale_spinner_clicked)

    # Digital Dash Scale Label
    digital_dash_scale_label = ac.addLabel(config_window, "Digital Dash Scale")
    ac.setPosition(digital_dash_scale_label, 19 * config_scale, 336 * config_scale)
    ac.setFontSize(digital_dash_scale_label, 25 * config_scale)
    ac.setCustomFont(digital_dash_scale_label, "Strait", 0, 0)
    ac.setFontColor(digital_dash_scale_label, 1, 1, 1, 1)

    # Digital Dash Scale Spinner
    digital_dash_scale_spinner = ac.addSpinner(config_window, "")
    ac.setRange(digital_dash_scale_spinner, 30, 200)
    ac.setStep(digital_dash_scale_spinner, 1)
    ac.setValue(digital_dash_scale_spinner, digital_dash_scale * 100)
    ac.setPosition(digital_dash_scale_spinner, 350 * config_scale, 337 * config_scale)
    ac.setSize(digital_dash_scale_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(digital_dash_scale_spinner, 22 * config_scale)
    ac.setCustomFont(digital_dash_scale_spinner, "Strait", 0, 0)
    ac.setFontColor(digital_dash_scale_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(digital_dash_scale_spinner, digital_dash_scale_spinner_clicked)

    # Turbo Gauge Scale Label
    turbo_gauge_scale_label = ac.addLabel(config_window, "Turbo Gauge Scale")
    ac.setPosition(turbo_gauge_scale_label, 19 * config_scale, 376 * config_scale)
    ac.setFontSize(turbo_gauge_scale_label, 25 * config_scale)
    ac.setCustomFont(turbo_gauge_scale_label, "Strait", 0, 0)
    ac.setFontColor(turbo_gauge_scale_label, 1, 1, 1, 1)

    # Turbo Gauge Scale Spinner
    turbo_gauge_scale_spinner = ac.addSpinner(config_window, "")
    ac.setRange(turbo_gauge_scale_spinner, 30, 200)
    ac.setStep(turbo_gauge_scale_spinner, 1)
    ac.setValue(turbo_gauge_scale_spinner, turbo_gauge_scale * 100)
    ac.setPosition(turbo_gauge_scale_spinner, 350 * config_scale, 377 * config_scale)
    ac.setSize(turbo_gauge_scale_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(turbo_gauge_scale_spinner, 22 * config_scale)
    ac.setCustomFont(turbo_gauge_scale_spinner, "Strait", 0, 0)
    ac.setFontColor(turbo_gauge_scale_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(turbo_gauge_scale_spinner, turbo_gauge_scale_spinner_clicked)

    # Map Overlay Scale Label
    map_overlay_scale_label = ac.addLabel(config_window, "Map Overlay Scale")
    ac.setPosition(map_overlay_scale_label, 19 * config_scale, 416 * config_scale)
    ac.setFontSize(map_overlay_scale_label, 25 * config_scale)
    ac.setCustomFont(map_overlay_scale_label, "Strait", 0, 0)
    ac.setFontColor(map_overlay_scale_label, 1, 1, 1, 1)

    # Map Overlay Scale Spinner
    map_overlay_scale_spinner = ac.addSpinner(config_window, "")
    ac.setRange(map_overlay_scale_spinner, 30, 200)
    ac.setStep(map_overlay_scale_spinner, 1)
    ac.setValue(map_overlay_scale_spinner, map_overlay_scale * 100)
    ac.setPosition(map_overlay_scale_spinner, 350 * config_scale, 417 * config_scale)
    ac.setSize(map_overlay_scale_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(map_overlay_scale_spinner, 22 * config_scale)
    ac.setCustomFont(map_overlay_scale_spinner, "Strait", 0, 0)
    ac.setFontColor(map_overlay_scale_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(map_overlay_scale_spinner, map_overlay_scale_spinner_clicked)

    # Position Overlays Scale Label
    player_rivals_overlays_scale_label = ac.addLabel(config_window, "Player/Rivals Overlays Scale")
    ac.setPosition(player_rivals_overlays_scale_label, 19 * config_scale, 456 * config_scale)
    ac.setFontSize(player_rivals_overlays_scale_label, 25 * config_scale)
    ac.setCustomFont(player_rivals_overlays_scale_label, "Strait", 0, 0)
    ac.setFontColor(player_rivals_overlays_scale_label, 1, 1, 1, 1)

    # Position Overlays Scale Spinner
    player_rivals_overlays_scale_spinner = ac.addSpinner(config_window, "")
    ac.setRange(player_rivals_overlays_scale_spinner, 30, 200)
    ac.setStep(player_rivals_overlays_scale_spinner, 1)
    ac.setValue(player_rivals_overlays_scale_spinner, player_rivals_overlays_scale * 100)
    ac.setPosition(player_rivals_overlays_scale_spinner, 350 * config_scale, 457 * config_scale)
    ac.setSize(player_rivals_overlays_scale_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(player_rivals_overlays_scale_spinner, 22 * config_scale)
    ac.setCustomFont(player_rivals_overlays_scale_spinner, "Strait", 0, 0)
    ac.setFontColor(player_rivals_overlays_scale_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(player_rivals_overlays_scale_spinner, player_rivals_overlays_scale_spinner_clicked)

    # Rear View Mirror Overlay Scale Label
    rear_view_mirror_overlay_scale_label = ac.addLabel(config_window, "Rear View Mirror Overlay Scale")
    ac.setPosition(rear_view_mirror_overlay_scale_label, 19 * config_scale, 496 * config_scale)
    ac.setFontSize(rear_view_mirror_overlay_scale_label, 25 * config_scale)
    ac.setCustomFont(rear_view_mirror_overlay_scale_label, "Strait", 0, 0)
    ac.setFontColor(rear_view_mirror_overlay_scale_label, 1, 1, 1, 1)

    # Rear View Mirror Overlay Scale Spinner
    rear_view_mirror_overlay_scale_spinner = ac.addSpinner(config_window, "")
    ac.setRange(rear_view_mirror_overlay_scale_spinner, 50, 200)
    ac.setStep(rear_view_mirror_overlay_scale_spinner, 1)
    ac.setValue(rear_view_mirror_overlay_scale_spinner, rear_view_mirror_overlay_scale * 100)
    ac.setPosition(rear_view_mirror_overlay_scale_spinner, 350 * config_scale, 497 * config_scale)
    ac.setSize(rear_view_mirror_overlay_scale_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(rear_view_mirror_overlay_scale_spinner, 22 * config_scale)
    ac.setCustomFont(rear_view_mirror_overlay_scale_spinner, "Strait", 0, 0)
    ac.setFontColor(rear_view_mirror_overlay_scale_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(rear_view_mirror_overlay_scale_spinner, rear_view_mirror_overlay_scale_spinner_clicked)

    # Time Counter Scale Label
    time_scale_label = ac.addLabel(config_window, "Time Counter Scale")
    ac.setPosition(time_scale_label, 19 * config_scale, 536 * config_scale)
    ac.setFontSize(time_scale_label, 25 * config_scale)
    ac.setCustomFont(time_scale_label, "Strait", 0, 0)
    ac.setFontColor(time_scale_label, 1, 1, 1, 1)

    # Time Counter Scale Spinner
    time_scale_spinner = ac.addSpinner(config_window, "")
    ac.setRange(time_scale_spinner, 10, 200)
    ac.setStep(time_scale_spinner, 1)
    ac.setValue(time_scale_spinner, time_scale * 100)
    ac.setPosition(time_scale_spinner, 350 * config_scale, 537 * config_scale)
    ac.setSize(time_scale_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(time_scale_spinner, 22 * config_scale)
    ac.setCustomFont(time_scale_spinner, "Strait", 0, 0)
    ac.setFontColor(time_scale_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(time_scale_spinner, time_scale_spinner_clicked)


    # Player/Rivals Customization Label
    player_rivals_customization_label = ac.addLabel(config_window, "Player/Rivals Customization")
    ac.setPosition(player_rivals_customization_label, 485 * config_scale,202 * config_scale)
    ac.setFontSize(player_rivals_customization_label, 37 * config_scale)
    ac.setCustomFont(player_rivals_customization_label, "Strait", 0, 1)
    ac.setFontColor(player_rivals_customization_label, 1, 1, 1, 1)
    
    # Player Label
    player_label = ac.addLabel(config_window, "Player")
    ac.setPosition(player_label, 604 * config_scale, 258 * config_scale)
    ac.setFontSize(player_label, 25 * config_scale)
    ac.setCustomFont(player_label, "Strait", 0, 0)
    ac.setFontColor(player_label, 1, 1, 1, 1)

    # Rival 1 Label
    rival_1_label = ac.addLabel(config_window, "Rival 1")
    ac.setPosition(rival_1_label, 704 * config_scale, 258 * config_scale)
    ac.setFontSize(rival_1_label, 25 * config_scale)
    ac.setCustomFont(rival_1_label, "Strait", 0, 0)
    ac.setFontColor(rival_1_label, 1, 1, 1, 1)

    # Rival 2 Label
    rival_2_label = ac.addLabel(config_window, "Rival 2")
    ac.setPosition(rival_2_label, 804 * config_scale, 258 * config_scale)
    ac.setFontSize(rival_2_label, 25 * config_scale)
    ac.setCustomFont(rival_2_label, "Strait", 0, 0)
    ac.setFontColor(rival_2_label, 1, 1, 1, 1)

    # Rival 3 Label
    rival_3_label = ac.addLabel(config_window, "Rival 3")
    ac.setPosition(rival_3_label, 904 * config_scale, 258 * config_scale)
    ac.setFontSize(rival_3_label, 25 * config_scale)
    ac.setCustomFont(rival_3_label, "Strait", 0, 0)
    ac.setFontColor(rival_3_label, 1, 1, 1, 1)

    # Icon Label
    icon_label = ac.addLabel(config_window, "Icon")
    ac.setPosition(icon_label, 479 * config_scale, 291 * config_scale)
    ac.setFontSize(icon_label, 32 * config_scale)
    ac.setCustomFont(icon_label, "Strait", 0, 0)
    ac.setFontColor(icon_label, 1, 1, 1, 1)

    # Color Label
    color_label = ac.addLabel(config_window, "Color")
    ac.setPosition(color_label, 479 * config_scale, 330 * config_scale)
    ac.setFontSize(color_label, 32 * config_scale)
    ac.setCustomFont(color_label, "Strait", 0, 0)
    ac.setFontColor(color_label, 1, 1, 1, 1)

    # Type Label
    type_label = ac.addLabel(config_window, "Type")
    ac.setPosition(type_label, 480 * config_scale, 371 * config_scale)
    ac.setFontSize(type_label, 32 * config_scale)
    ac.setCustomFont(type_label, "Strait", 0, 0)
    ac.setFontColor(type_label, 1, 1, 1, 1)

    # Player Character Icon Selector
    change_player_icon_spinner = ac.addSpinner(config_window, "")
    ac.setRange(change_player_icon_spinner, 0, 61)
    ac.setStep(change_player_icon_spinner, 1)
    ac.setValue(change_player_icon_spinner, player_icon_number)
    ac.setPosition(change_player_icon_spinner, 593 * config_scale, 297 * config_scale)
    ac.setSize(change_player_icon_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(change_player_icon_spinner, 22 * config_scale)
    ac.setCustomFont(change_player_icon_spinner, "Strait", 0, 0)
    ac.setFontColor(change_player_icon_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(change_player_icon_spinner, change_player_icon_spinner_clicked)

    # Rival 1st Character Icon Selector
    change_rival_1st_icon_spinner = ac.addSpinner(config_window, "")
    ac.setRange(change_rival_1st_icon_spinner, 0, 61)
    ac.setStep(change_rival_1st_icon_spinner, 1)
    ac.setValue(change_rival_1st_icon_spinner, rival_1st_icon_number)
    ac.setPosition(change_rival_1st_icon_spinner, 695 * config_scale, 297 * config_scale)
    ac.setSize(change_rival_1st_icon_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(change_rival_1st_icon_spinner, 22 * config_scale)
    ac.setCustomFont(change_rival_1st_icon_spinner, "Strait", 0, 0)
    ac.setFontColor(change_rival_1st_icon_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(change_rival_1st_icon_spinner, change_rival_1st_icon_spinner_clicked)

    # Rival 2nd Character Icon Selector
    change_rival_2nd_icon_spinner = ac.addSpinner(config_window, "")
    ac.setRange(change_rival_2nd_icon_spinner, 0, 61)
    ac.setStep(change_rival_2nd_icon_spinner, 1)
    ac.setValue(change_rival_2nd_icon_spinner, rival_2nd_icon_number)
    ac.setPosition(change_rival_2nd_icon_spinner, 795 * config_scale, 297 * config_scale)
    ac.setSize(change_rival_2nd_icon_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(change_rival_2nd_icon_spinner, 22 * config_scale)
    ac.setCustomFont(change_rival_2nd_icon_spinner, "Strait", 0, 0)
    ac.setFontColor(change_rival_2nd_icon_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(change_rival_2nd_icon_spinner, change_rival_2nd_icon_spinner_clicked)

    # Rival 3rd Character Icon Selector
    change_rival_3rd_icon_spinner = ac.addSpinner(config_window, "")
    ac.setRange(change_rival_3rd_icon_spinner, 0, 61)
    ac.setStep(change_rival_3rd_icon_spinner, 1)
    ac.setValue(change_rival_3rd_icon_spinner, rival_3rd_icon_number)
    ac.setPosition(change_rival_3rd_icon_spinner, 895 * config_scale, 297 * config_scale)
    ac.setSize(change_rival_3rd_icon_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(change_rival_3rd_icon_spinner, 22 * config_scale)
    ac.setCustomFont(change_rival_3rd_icon_spinner, "Strait", 0, 0)
    ac.setFontColor(change_rival_3rd_icon_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(change_rival_3rd_icon_spinner, change_rival_3rd_icon_spinner_clicked)

    # Player Overlay Background Color
    player_overlay_background_color_spinner = ac.addSpinner(config_window, "")
    ac.setRange(player_overlay_background_color_spinner, 0, len(color_presets) - 1)
    ac.setStep(player_overlay_background_color_spinner, 1)
    ac.setValue(player_overlay_background_color_spinner, player_overlay_background_color_index)
    ac.setPosition(player_overlay_background_color_spinner, 593 * config_scale, 337 * config_scale)
    ac.setSize(player_overlay_background_color_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(player_overlay_background_color_spinner, 22 * config_scale)
    ac.setCustomFont(player_overlay_background_color_spinner, "Strait", 0, 0)
    ac.setFontColor(player_overlay_background_color_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(player_overlay_background_color_spinner, player_overlay_background_color_spinner_clicked)

    # Rival 1st Overlay Background Color
    rival_1st_overlay_background_color_spinner = ac.addSpinner(config_window, "")
    ac.setRange(rival_1st_overlay_background_color_spinner, 0, len(color_presets) - 1)
    ac.setStep(rival_1st_overlay_background_color_spinner, 1)
    ac.setValue(rival_1st_overlay_background_color_spinner, rival_1st_overlay_background_color_index)
    ac.setPosition(rival_1st_overlay_background_color_spinner, 694 * config_scale, 337 * config_scale)
    ac.setSize(rival_1st_overlay_background_color_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(rival_1st_overlay_background_color_spinner, 22 * config_scale)
    ac.setCustomFont(rival_1st_overlay_background_color_spinner, "Strait", 0, 0)
    ac.setFontColor(rival_1st_overlay_background_color_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(rival_1st_overlay_background_color_spinner, rival_1st_overlay_background_color_spinner_clicked)

    # Rival 2nd Overlay Background Color
    rival_2nd_overlay_background_color_spinner = ac.addSpinner(config_window, "")
    ac.setRange(rival_2nd_overlay_background_color_spinner, 0, len(color_presets) - 1)
    ac.setStep(rival_2nd_overlay_background_color_spinner, 1)
    ac.setValue(rival_2nd_overlay_background_color_spinner, rival_2nd_overlay_background_color_index)
    ac.setPosition(rival_2nd_overlay_background_color_spinner, 794 * config_scale, 337 * config_scale)
    ac.setSize(rival_2nd_overlay_background_color_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(rival_2nd_overlay_background_color_spinner, 22 * config_scale)
    ac.setCustomFont(rival_2nd_overlay_background_color_spinner, "Strait", 0, 0)
    ac.setFontColor(rival_2nd_overlay_background_color_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(rival_2nd_overlay_background_color_spinner, rival_2nd_overlay_background_color_spinner_clicked)

    # Rival 3rd Overlay Background Color
    rival_3rd_overlay_background_color_spinner = ac.addSpinner(config_window, "")
    ac.setRange(rival_3rd_overlay_background_color_spinner, 0, len(color_presets) - 1)
    ac.setStep(rival_3rd_overlay_background_color_spinner, 1)
    ac.setValue(rival_3rd_overlay_background_color_spinner, rival_3rd_overlay_background_color_index)
    ac.setPosition(rival_3rd_overlay_background_color_spinner, 894 * config_scale, 337 * config_scale)
    ac.setSize(rival_3rd_overlay_background_color_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(rival_3rd_overlay_background_color_spinner, 22 * config_scale)
    ac.setCustomFont(rival_3rd_overlay_background_color_spinner, "Strait", 0, 0)
    ac.setFontColor(rival_3rd_overlay_background_color_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(rival_3rd_overlay_background_color_spinner, rival_3rd_overlay_background_color_spinner_clicked)

    # Player Type Selector
    change_player_type_spinner = ac.addSpinner(config_window, "")
    ac.setRange(change_player_type_spinner, 0, 15)
    ac.setStep(change_player_type_spinner, 1)
    ac.setValue(change_player_type_spinner, player_type)
    ac.setPosition(change_player_type_spinner, 593 * config_scale, 377 * config_scale)
    ac.setSize(change_player_type_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(change_player_type_spinner, 22 * config_scale)
    ac.setCustomFont(change_player_type_spinner, "Strait", 0, 0)
    ac.setFontColor(change_player_type_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(change_player_type_spinner, change_player_type_spinner_clicked)

    # Rival 1st Type Selector
    change_rival_1st_type_spinner = ac.addSpinner(config_window, "")
    ac.setRange(change_rival_1st_type_spinner, 0, 15)
    ac.setStep(change_rival_1st_type_spinner, 1)
    ac.setValue(change_rival_1st_type_spinner, rival_1st_type)
    ac.setPosition(change_rival_1st_type_spinner, 694 * config_scale, 377 * config_scale)
    ac.setSize(change_rival_1st_type_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(change_rival_1st_type_spinner, 22 * config_scale)
    ac.setCustomFont(change_rival_1st_type_spinner, "Strait", 0, 0)
    ac.setFontColor(change_rival_1st_type_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(change_rival_1st_type_spinner, change_rival_1st_type_spinner_clicked)

    # Rival 2nd Type Selector
    change_rival_2nd_type_spinner = ac.addSpinner(config_window, "")
    ac.setRange(change_rival_2nd_type_spinner, 0, 15)
    ac.setStep(change_rival_2nd_type_spinner, 1)
    ac.setValue(change_rival_2nd_type_spinner, rival_2nd_type)
    ac.setPosition(change_rival_2nd_type_spinner, 794 * config_scale, 377 * config_scale)
    ac.setSize(change_rival_2nd_type_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(change_rival_2nd_type_spinner, 22 * config_scale)
    ac.setCustomFont(change_rival_2nd_type_spinner, "Strait", 0, 0)
    ac.setFontColor(change_rival_2nd_type_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(change_rival_2nd_type_spinner, change_rival_2nd_type_spinner_clicked)

    # Rival 3rd Type Selector
    change_rival_3rd_type_spinner = ac.addSpinner(config_window, "")
    ac.setRange(change_rival_3rd_type_spinner, 0, 15)
    ac.setStep(change_rival_3rd_type_spinner, 1)
    ac.setValue(change_rival_3rd_type_spinner, rival_3rd_type)
    ac.setPosition(change_rival_3rd_type_spinner, 894 * config_scale, 377 * config_scale)
    ac.setSize(change_rival_3rd_type_spinner, 89 * config_scale, 29 * config_scale)
    ac.setFontSize(change_rival_3rd_type_spinner, 22 * config_scale)
    ac.setCustomFont(change_rival_3rd_type_spinner, "Strait", 0, 0)
    ac.setFontColor(change_rival_3rd_type_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(change_rival_3rd_type_spinner, change_rival_3rd_type_spinner_clicked)


    # Opacity Label
    opacity_label = ac.addLabel(config_window, "Opacity")
    ac.setPosition(opacity_label, 477 * config_scale, 444 * config_scale)
    ac.setFontSize(opacity_label, 37 * config_scale)
    ac.setCustomFont(opacity_label, "Strait", 0, 1)
    ac.setFontColor(opacity_label, 1, 1, 1, 1)

    # Tachometer Background Opacity Label
    tachometer_background_opacity_label = ac.addLabel(config_window, "Tachometer Background Opacity")
    ac.setPosition(tachometer_background_opacity_label, 480 * config_scale, 496 * config_scale)
    ac.setFontSize(tachometer_background_opacity_label, 25 * config_scale)
    ac.setCustomFont(tachometer_background_opacity_label, "Strait", 0, 0)
    ac.setFontColor(tachometer_background_opacity_label, 1, 1, 1, 1)

    # Tachometer Background Opacity Spinner
    tachometer_background_opacity_spinner = ac.addSpinner(config_window, "")
    ac.setRange(tachometer_background_opacity_spinner, 0, len(opacity_presets) - 1)
    ac.setStep(tachometer_background_opacity_spinner, 1)
    ac.setValue(tachometer_background_opacity_spinner, tachometer_background_opacity_index)
    ac.setPosition(tachometer_background_opacity_spinner, 867 * config_scale, 496 * config_scale)
    ac.setSize(tachometer_background_opacity_spinner, 117 * config_scale, 29 * config_scale)
    ac.setFontSize(tachometer_background_opacity_spinner, 25 * config_scale)
    ac.setCustomFont(tachometer_background_opacity_spinner, "Strait", 0, 0)
    ac.setFontColor(tachometer_background_opacity_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(tachometer_background_opacity_spinner, tachometer_background_opacity_spinner_clicked)

    # Turbo Background Opacity Label
    turbo_background_opacity_label = ac.addLabel(config_window, "Turbo Background Opacity")
    ac.setPosition(turbo_background_opacity_label, 482 * config_scale, 537 * config_scale)
    ac.setFontSize(turbo_background_opacity_label, 25 * config_scale)
    ac.setCustomFont(turbo_background_opacity_label, "Strait", 0, 0)
    ac.setFontColor(turbo_background_opacity_label, 1, 1, 1, 1)

    # Turbo Gauge Background Opacity Spinner
    turbo_gauge_background_opacity_spinner = ac.addSpinner(config_window, "")
    ac.setRange(turbo_gauge_background_opacity_spinner, 0, len(opacity_presets) - 1)
    ac.setStep(turbo_gauge_background_opacity_spinner, 1)
    ac.setValue(turbo_gauge_background_opacity_spinner, turbo_gauge_background_opacity_index)
    ac.setPosition(turbo_gauge_background_opacity_spinner, 867 * config_scale, 537 * config_scale)
    ac.setSize(turbo_gauge_background_opacity_spinner, 117 * config_scale, 29 * config_scale)
    ac.setFontSize(turbo_gauge_background_opacity_spinner, 25 * config_scale)
    ac.setCustomFont(turbo_gauge_background_opacity_spinner, "Strait", 0, 0)
    ac.setFontColor(turbo_gauge_background_opacity_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(turbo_gauge_background_opacity_spinner, turbo_gauge_background_opacity_spinner_clicked)

    # Player/Rivals Preset Label
    player_rivals_preset_label = ac.addLabel(config_window, "Player/Rivals Preset")
    ac.setPosition(player_rivals_preset_label, 26 * config_scale, 638 * config_scale)
    ac.setFontSize(player_rivals_preset_label, 25 * config_scale)
    ac.setCustomFont(player_rivals_preset_label, "Strait", 0, 0)
    ac.setFontColor(player_rivals_preset_label, 1, 1, 1, 1)

    # Player/Rival Type Position Index Spinner
    player_rivals_type_position_spinner = ac.addSpinner(config_window, "")
    ac.setRange(player_rivals_type_position_spinner, 0, len(configurations_rival_type_presets) - 1)
    ac.setRange(player_rivals_type_position_spinner, 0, len(configurations_player_type_presets) - 1)
    ac.setStep(player_rivals_type_position_spinner, 1)
    ac.setValue(player_rivals_type_position_spinner, player_rivals_type_position_index)
    ac.setPosition(player_rivals_type_position_spinner, 350 * config_scale, 639 * config_scale)
    ac.setSize(player_rivals_type_position_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(player_rivals_type_position_spinner, 22 * config_scale)
    ac.setCustomFont(player_rivals_type_position_spinner, "Strait", 0, 0)
    ac.setFontColor(player_rivals_type_position_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(player_rivals_type_position_spinner, player_rivals_type_position_spinner_clicked)

    # Player Mode Type Label
    player_mode_type_label = ac.addLabel(config_window, "Player Mode Type")
    ac.setPosition(player_mode_type_label, 26 * config_scale, 678 * config_scale)
    ac.setFontSize(player_mode_type_label, 25 * config_scale)
    ac.setCustomFont(player_mode_type_label, "Strait", 0, 0)
    ac.setFontColor(player_mode_type_label, 1, 1, 1, 1)

    # Player Mode Type Spinner
    player_mode_type_spinner = ac.addSpinner(config_window, "")
    ac.setRange(player_mode_type_spinner, 0, 20)
    ac.setStep(player_mode_type_spinner, 1)
    ac.setValue(player_mode_type_spinner, player_mode_type)
    ac.setPosition(player_mode_type_spinner, 350 * config_scale, 680 * config_scale)
    ac.setSize(player_mode_type_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(player_mode_type_spinner, 22 * config_scale)
    ac.setCustomFont(player_mode_type_spinner, "Strait", 0, 0)
    ac.setFontColor(player_mode_type_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(player_mode_type_spinner, player_mode_type_spinner_clicked)

    # Player Mode Position Label
    player_mode_position_label = ac.addLabel(config_window, "Player Mode Position")
    ac.setPosition(player_mode_position_label, 26 * config_scale, 718 * config_scale)
    ac.setFontSize(player_mode_position_label, 25 * config_scale)
    ac.setCustomFont(player_mode_position_label, "Strait", 0, 0)
    ac.setFontColor(player_mode_position_label, 1, 1, 1, 1)

    # Player Mode Type Position Index Spinner
    player_mode_position_spinner = ac.addSpinner(config_window, "")
    ac.setRange(player_mode_position_spinner, 0, len(configurations_player_mode_type) - 1)
    ac.setStep(player_mode_position_spinner, 1)
    ac.setValue(player_mode_position_spinner, mode_type_position_index)
    ac.setPosition(player_mode_position_spinner, 350 * config_scale, 720 * config_scale)
    ac.setSize(player_mode_position_spinner, 117 * config_scale, 25 * config_scale)
    ac.setFontSize(player_mode_position_spinner, 22 * config_scale)
    ac.setCustomFont(player_mode_position_spinner, "Strait", 0, 0)
    ac.setFontColor(player_mode_position_spinner, 1, 1, 1, 1)
    ac.addOnValueChangeListener(player_mode_position_spinner, player_mode_position_spinner_clicked)


    # Show Player/Rivals Current Positions Checkbox Label
    show_player_rivals_current_position_label = ac.addLabel(config_window, "Show Player/Rivals Current Position")
    ac.setPosition(show_player_rivals_current_position_label, 482 * config_scale, 638 * config_scale)
    ac.setFontSize(show_player_rivals_current_position_label, 25 * config_scale)
    ac.setCustomFont(show_player_rivals_current_position_label, "Strait", 0, 0)
    ac.setFontColor(show_player_rivals_current_position_label, 1, 1, 1, 1)

    # Show Player/Rivals Current Positions Checkbox
    show_player_rivals_current_position_checkbox = ac.addCheckBox(config_window, "")
    ac.setValue(show_player_rivals_current_position_checkbox, show_player_rivals_current_positions)
    ac.setPosition(show_player_rivals_current_position_checkbox, 953 * config_scale, 638 * config_scale)
    ac.setSize(show_player_rivals_current_position_checkbox, 100 * config_scale, 31 * config_scale)
    ac.addOnCheckBoxChanged(show_player_rivals_current_position_checkbox, show_player_rivals_current_positions_checkbox_clicked)

    # Show Map Overlay Text Checkbox Label
    show_map_overlay_text_label = ac.addLabel(config_window, "Show Map Overlay Text")
    ac.setPosition(show_map_overlay_text_label, 482 * config_scale, 683 * config_scale)
    ac.setFontSize(show_map_overlay_text_label, 25 * config_scale)
    ac.setCustomFont(show_map_overlay_text_label, "Strait", 0, 0)
    ac.setFontColor(show_map_overlay_text_label, 1, 1, 1, 1)

    # Show Map Overlay Text Checkbox
    show_map_overlay_text_checkbox = ac.addCheckBox(config_window, "")
    ac.setValue(show_map_overlay_text_checkbox, show_map_overlay_text)
    ac.setPosition(show_map_overlay_text_checkbox, 953 * config_scale, 681 * config_scale)
    ac.setSize(show_map_overlay_text_checkbox, 100 * config_scale, 31 * config_scale)
    ac.addOnCheckBoxChanged(show_map_overlay_text_checkbox, show_map_overlay_text_checkbox_clicked)

    # Reset All Button
    reset_all_button = ac.addButton(config_window, "Reset All (Restart)")
    ac.setPosition(reset_all_button, 25 * config_scale, 795 * config_scale)
    ac.setSize(reset_all_button, 150 * config_scale, 27 * config_scale)
    ac.setFontSize(reset_all_button, 19 * config_scale)
    ac.setCustomFont(reset_all_button, "Strait", 0, 0)
    ac.setFontColor(reset_all_button, 1, 1, 1, 1)
    ac.addOnClickedListener(reset_all_button, reset_all_button_clicked)

    ac.addRenderCallback(config_window, configGL)
    ac.addRenderCallback(tachometer_window, tachometerGL)
    ac.addRenderCallback(digital_dash_window, digitaldashGL)
    ac.addRenderCallback(turbo_gauge_window, turbogaugeGL)
    ac.addRenderCallback(map_overlay_window, mapoverlayGL)
    ac.addRenderCallback(player_overlay_window, playeroverlayGL)
    ac.addRenderCallback(rivals_overlay_window, rivalsoverlayGL)
    ac.addRenderCallback(rear_view_mirror_overlay_window, rearviewmirrorGL)
    ac.addRenderCallback(time_counter_window, timecounterGL)


def configGL(deltaT):
    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # AC X WMPS3 Logo Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(acxwmps3_logo)
    config_vertex_tex(300, 5, 440, 99)

    ac.glEnd()


def tachometerGL(deltaT):
    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # Tachometer Background Opacity
    tachometer_background_opacity = opacity_presets[tachometer_background_opacity_index]

    # Tachometer Background Image
    ac.glColor4f(*tachometer_background_opacity)
    ac.ext_glSetTexture(tachometer_background)
    tachometer_vertex_tex(0, 0, 500, 500)

    # Tachometer Carbon Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(tachometer_carbon)
    tachometer_vertex_tex(0, 0, 500, 500)

    # Tachometer RPM States Images
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(tachometer_rpm[state])
    tachometer_vertex_tex(0, 0, 500, 500)

    # Tachometer Needle Logic
    tachometer_needle_degree_offset = -144
    tachometer_needle_degree_available = 287.5
    tachometer_spin_rate = redRpm_state[state] / tachometer_needle_degree_available
    tachometer_needle_degree = rpm / tachometer_spin_rate + tachometer_needle_degree_offset

    # Tachometer Needle Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(tachometer_needle)
    tachometer_needle_rotate(0, 0, 500, 500, 250, 250, tachometer_needle_degree)

    # Tachometer Dot Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(tachometer_dot)
    tachometer_vertex_tex(0, 0, 500, 500)

    ac.glEnd()


def digitaldashGL(deltaT):
    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # Digital Dash Background Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(digital_dash_background)
    digital_dash_vertex_tex(0, 0, 864, 247)

    # Digital Dash Gears Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(digital_gears)
    digital_dash_vertex_tex(42, -5, 143.27, 166.93, gear /11, (gear + 1) / 11)

    # Digital Speed Numbers Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(digital_speed_numbers)
    digit_width = 147.29
    digit_height = 172.70
    total_digits = len(speed_list)
    start_x = 563

    for i, digit in enumerate((speed_list)):
        x = start_x - (total_digits - i - 1) * digit_width / 1.22
        y = - 3
        coord_x1 = int(digit) / 10
        coord_x2 = 0.1 + (int(digit) / 10)
        digital_dash_vertex_tex(x, y, digit_width, digit_height, coord_x1, coord_x2)

    # Speed UoM Image Logic
    if uom_kmh:
        vertex_y = 0
    else:
        vertex_y = 0.5

    # Speed UOM Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(speed_uom)
    digital_dash_vertex_tex(685, 103, 179, 67, 0, 1, vertex_y, vertex_y + 0.5)

    # Auto Manual Image Logic
    if auto_on:
        vertex_y = 0
    else:
        vertex_y = 0.5

    # Auto Manual Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(auto_manual)
    digital_dash_vertex_tex(153, 105, 176, 64, 0, 1, vertex_y, vertex_y + 0.5)

    # Handbrake Image Logic
    if handbrake_on:
        color = (1, 1, 1, 1)
    else:
        color = (0, 0, 0, 0)

    # Handbrake Image
    ac.glColor4f(*color)
    ac.ext_glSetTexture(handbrake)
    digital_dash_vertex_tex(187, 10.5, 137, 107)

    ac.glEnd()


def turbogaugeGL(deltaT):
    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # Turbo Gauge B@ckground Opacity
    turbo_gauge_background_opacity = opacity_presets[turbo_gauge_background_opacity_index]

    # Turbo Gauge Background Image
    ac.glColor4f(*turbo_gauge_background_opacity)
    ac.ext_glSetTexture(turbo_gauge_background)
    turbo_gauge_vertex_tex(0, 0, 300, 300)

    # Turbo Gauge Carbon Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(turbo_gauge_carbon)
    turbo_gauge_vertex_tex(0, 0, 300, 300)

    # Turbo Gauge Units
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(turbo_gauge_unit)

    turbo_gauge_vertex_tex(0, 0, 300, 300)

    # Turbo Gauge Needle Image
    degree_offset = -90
    degree_available = 60
    spin_rate = (degree_available / 3)
    turbo_gauge_degree = boost / spin_rate + degree_offset
    
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(turbo_gauge_needle)
    turbo_gauge_rotate(0, 0, 300, 300, 150, 150, degree_available, turbo_gauge_degree)

    # Tachometer Needle Dot Image
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(turbo_gauge_dot)
    turbo_gauge_vertex_tex(0, 0, 300, 300)

    ac.glEnd()


def mapoverlayGL(deltaT):
    global distance

    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # Show/Hide Map Text
    if show_map_overlay_text:
        map_text_color = (1, 1, 1, 1)
    else:
        map_text_color = (0, 0, 0, 0)

    # Map Overlay Background Image
    ac.glColor4f(1, 1, 1, 0.3)
    ac.ext_glSetTexture(map_overlay_background)
    map_overlay_vertex_tex(0, 0, 267, 267)

    # Map Overlay Text Logic
    if distance >= 10000:
        # XX (Thousands)
        ac.glColor4f(*map_text_color)
        ac.ext_glSetTexture(map_numbers)

        distance_digits_width = 11.7
        distance_digits_height = 22
        total_distance_digits = len(distance_xxdotx_xx_list)
        distance_width = total_distance_digits * distance_digits_width
        start_x = 180 + 8.5 - distance_width

        for i, distance_digits in enumerate(distance_xxdotx_xx_list):
            x = start_x + i * distance_digits_width
            y = 232
            coord_x1 = int(distance_digits) / 10
            coord_x2 = 0.1 + (int(distance_digits) / 10)
            map_overlay_vertex_tex(x, y, distance_digits_width, distance_digits_height, coord_x1, coord_x2)
        
        # X (Hundreds)
        ac.glColor4f(*map_text_color)
        ac.ext_glSetTexture(map_numbers)

        distance_digits_width = 11.7
        distance_digits_height = 22
        total_distance_digits = len(distance_xxdotx_x_list)
        distance_width = total_distance_digits * distance_digits_width
        start_x = 198 + 14.5 - distance_width

        for i, distance_digits in enumerate(distance_xxdotx_x_list):
            x = start_x + i * distance_digits_width
            y = 232
            coord_x1 = int(distance_digits) / 10
            coord_x2 = 0.1 + (int(distance_digits) / 10)
            map_overlay_vertex_tex(x, y, distance_digits_width, distance_digits_height, coord_x1, coord_x2)
    else:
        # XX (Thousands)
        ac.glColor4f(*map_text_color)
        ac.ext_glSetTexture(map_numbers)

        distance_digits_width = 11.7
        distance_digits_height = 22
        total_distance_digits = len(distance_xdotxx_x_list)
        distance_width = total_distance_digits * distance_digits_width
        start_x = 177 - distance_width

        for i, distance_digits in enumerate(distance_xdotxx_x_list):
            x = start_x + i * distance_digits_width
            y = 232
            coord_x1 = int(distance_digits) / 10
            coord_x2 = 0.1 + (int(distance_digits) / 10)
            map_overlay_vertex_tex(x, y, distance_digits_width, distance_digits_height, coord_x1, coord_x2)
        
        # X (Hundreds)
        ac.glColor4f(*map_text_color)
        ac.ext_glSetTexture(map_numbers)
        
        distance_digits_width = 11.7
        distance_digits_height = 22
        total_distance_digits = len(distance_xdotxx_xx_list)
        distance_width = total_distance_digits * distance_digits_width
        start_x = 206.5 + 6 - distance_width

        for i, distance_digits in enumerate(distance_xdotxx_xx_list):
            x = start_x + i * distance_digits_width
            y = 232
            coord_x1 = int(distance_digits) / 10
            coord_x2 = 0.1 + (int(distance_digits) / 10)
            map_overlay_vertex_tex(x, y, distance_digits_width, distance_digits_height, coord_x1, coord_x2)

    # Map Dot Image Logic
    dot_x = 1
    if distance >= 10000:
        dot_x += 12

    # Map Dot Image
    ac.glColor4f(*map_text_color)
    ac.ext_glSetTexture(map_dot)
    map_overlay_vertex_tex(dot_x, -2.5, 267, 267)

    # Map Stuff Image
    ac.glColor4f(*map_text_color)
    ac.ext_glSetTexture(map_stuff)
    map_overlay_vertex_tex(-3, 223, 268, 37)

    ac.glEnd()


def playeroverlayGL(deltaT):
    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # Player Overlay Background Color Logic
    player_overlay_background_color = color_presets[player_overlay_background_color_index]

    # Show/Hide Player Current Position
    if show_player_rivals_current_positions:
        color = (1, 1, 1, 1)
    else:
        color = (0, 0, 0, 0)

    # Parameter Player Logic
    parameter = configurations_player_type_presets[player_rivals_type_position_index]

    # Player Type Position and Scale connection to Parameters
    position_x = parameter['position_x'] + int(ac.getValue(player_rivals_type_position_spinner))
    position_y = parameter['position_y'] + int(ac.getValue(player_rivals_type_position_spinner))
    scale_x = parameter['scale_x'] + float(ac.getValue(player_rivals_type_position_spinner))
    scale_y = parameter['scale_y'] + float(ac.getValue(player_rivals_type_position_spinner))

    # Parameter Logic to Player Type Presets
    parameter_mode_type = configurations_player_mode_type[mode_type_position_index]

    # Parameter Logic to Player Mode Type Presets
    mode_type_position_x = parameter_mode_type['position_x'] + int(ac.getValue(player_mode_position_spinner))
    mode_type_position_y = parameter_mode_type['position_y'] + int(ac.getValue(player_mode_position_spinner))
    mode_type_scale_x = parameter_mode_type['scale_x'] + float(ac.getValue(player_mode_position_spinner))
    mode_type_scale_y = parameter_mode_type['scale_y'] + float(ac.getValue(player_mode_position_spinner))

    # Player Overlay Background and Color
    ac.glColor4f(*player_overlay_background_color)
    ac.ext_glSetTexture(position_overlay_player_background)
    player_rivals_overlays_vertex_tex(0, 0, 560, 120)

    # Player Type Logic
    selected_value = int(ac.getValue(change_player_type_spinner))
    player_type = selected_value
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(rival_text_selected[player_type])
    player_rivals_overlays_vertex_tex(position_x, position_y, scale_x, scale_y)

    # Player Mode Type Logic
    selected_value = int(ac.getValue(player_mode_type_spinner))
    player_mode_type = selected_value
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(player_mode_type_selected[player_mode_type])
    player_rivals_overlays_vertex_tex(mode_type_position_x, mode_type_position_y, mode_type_scale_x, mode_type_scale_y)

    # Player Icon Selection
    selected_value = int(ac.getValue(change_player_icon_spinner))

    # Player Icon Selected
    player_icon_number = selected_value

    # Player Icon Selected Detection from Selection Logic
    if 0 <= player_icon_number < len(player_icon_selected):
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(player_icon_selected[player_icon_number])
        player_rivals_overlays_vertex_tex(8.5, 8.5, 192, 103)
    else:
        return
    
    # Player Position Number based on Status and Session Logic
    if status == 2 and session_type == 2:
        if cars_in_session == 1:
            return
        else:
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)

            number_width = 150
            number_height = 70
            total_numbers = len(player_rivals_overlay_current_list)
            current_width = total_numbers * number_width
            start_x = 550 - current_width

            for i, position in enumerate(player_rivals_overlay_current_list):
                x = start_x + i * number_width
                y = 14
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)
    elif status == 1 and session_type == 2:
        if cars_in_session == 1:
            return
        else:
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)

            number_width = 150
            number_height = 70
            total_numbers = len(player_rivals_overlay_current_list)
            current_width = total_numbers * number_width
            start_x = 550 - current_width

            for i, position in enumerate(player_rivals_overlay_current_list):
                x = start_x + i * number_width
                y = 14
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)
    else:
        return


    ac.glEnd()


def rivalsoverlayGL(deltaT):

    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # Rival Overlays Background Color Selected from Presets
    rival_1st_overlay_background_color = color_presets[rival_1st_overlay_background_color_index]
    rival_2nd_overlay_background_color = color_presets[rival_2nd_overlay_background_color_index]
    rival_3rd_overlay_background_color = color_presets[rival_3rd_overlay_background_color_index]

    # Show/Hide Rivals Current Positions Logic
    if show_player_rivals_current_positions:
        color = (1, 1, 1, 1)
    else:
        color = (0, 0, 0, 0)

    # Parameter Rival Logic
    parameter = configurations_rival_type_presets[player_rivals_type_position_index]

    # Rivals Type Position and Scale connection to Parameters
    position_x = parameter['position_x'] + int(ac.getValue(player_rivals_type_position_spinner))
    position_y = parameter['position_y'] + int(ac.getValue(player_rivals_type_position_spinner))
    scale_x = parameter['scale_x'] + float(ac.getValue(player_rivals_type_position_spinner))
    scale_y = parameter['scale_y'] + float(ac.getValue(player_rivals_type_position_spinner))

    # Rivals Backgrounds Logic
    if cars_in_session == 1:
        return
    # Position Overlay Background For 1 Rival
    elif cars_in_session == 2:
        # 1st Rival Background
        ac.glColor4f(*rival_1st_overlay_background_color)
        ac.ext_glSetTexture(position_overlay_rivals_background)
        player_rivals_overlays_vertex_tex(0, 0, 560, 120)
    # Position Overlay Background For 2 Rivals
    elif cars_in_session == 3:
        # 1st Rival Background
        ac.glColor4f(*rival_1st_overlay_background_color)
        ac.ext_glSetTexture(position_overlay_rivals_background)
        player_rivals_overlays_vertex_tex(0, 0, 560, 120)
        # 2nd Rival Background
        ac.glColor4f(*rival_2nd_overlay_background_color)
        ac.ext_glSetTexture(position_overlay_rivals_background)
        player_rivals_overlays_vertex_tex(0, 112, 560, 120)
    # Position Overlay Background For 3 Rivals
    elif cars_in_session >= 4:
        # 1st Rival Background
        ac.glColor4f(*rival_1st_overlay_background_color)
        ac.ext_glSetTexture(position_overlay_rivals_background)
        player_rivals_overlays_vertex_tex(0, 0, 560, 120)
        # 2nd Rival Background
        ac.glColor4f(*rival_2nd_overlay_background_color)
        ac.ext_glSetTexture(position_overlay_rivals_background)
        player_rivals_overlays_vertex_tex(0, 112, 560, 120)
        # 3rd Rival Background
        ac.glColor4f(*rival_3rd_overlay_background_color)
        ac.ext_glSetTexture(position_overlay_rivals_background)
        player_rivals_overlays_vertex_tex(0, 224, 560, 120)
    else:
        return

    # Rivals Type Logic
    if cars_in_session == 1:
        return
    # Rivals Type for 1 Rival
    elif cars_in_session == 2:
        # 1st Rival Type
        selected_value = int(ac.getValue(change_rival_1st_type_spinner))
        rival_1st_type = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(rival_text_selected[rival_1st_type])
        player_rivals_overlays_vertex_tex(position_x, position_y, scale_x, scale_y)
    # Rivals Type for 2 Rivals
    elif cars_in_session == 3:
        # 1st Rival Type
        selected_value = int(ac.getValue(change_rival_1st_type_spinner))
        rival_1st_type = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(rival_text_selected[rival_1st_type])
        player_rivals_overlays_vertex_tex(position_x, position_y, scale_x, scale_y)
        # 2nd Rival Type
        selected_value = int(ac.getValue(change_rival_2nd_type_spinner))
        rival_2nd_type = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(rival_text_selected[rival_2nd_type])
        player_rivals_overlays_vertex_tex(position_x, position_y + 112, scale_x, scale_y  + 2)
    # Rivals Type for 3 Rivals
    elif cars_in_session >= 4:
        # 1st Rival Type
        selected_value = int(ac.getValue(change_rival_1st_type_spinner))
        rival_1st_type = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(rival_text_selected[rival_1st_type])
        player_rivals_overlays_vertex_tex(position_x, position_y, scale_x, scale_y)
        # 2nd Rival Type
        selected_value = int(ac.getValue(change_rival_2nd_type_spinner))
        rival_2nd_type = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(rival_text_selected[rival_2nd_type])
        player_rivals_overlays_vertex_tex(position_x, position_y + 112, scale_x, scale_y + 2)
        # 3rd Rival Type
        selected_value = int(ac.getValue(change_rival_3rd_type_spinner))
        rival_3rd_type = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(rival_text_selected[rival_3rd_type])
        player_rivals_overlays_vertex_tex(position_x, position_y + 225, scale_x, scale_y + 1)
    else:
        return

    # Rivals Icon Logic
    if cars_in_session == 1:
        return
    # Position Overlay Background For 1 Rival
    elif cars_in_session == 2:
        # 1st Rival Icon
        selected_value = int(ac.getValue(change_rival_1st_icon_spinner))
        rival_1st_icon_number = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(player_icon_selected[rival_1st_icon_number])
        player_rivals_overlays_vertex_tex(360, 8.5, 192, 103)
    # Position Overlay Background For 2 Rivals
    elif cars_in_session == 3:
        # 1st Rival Icon
        selected_value = int(ac.getValue(change_rival_1st_icon_spinner))
        rival_1st_icon_number = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(player_icon_selected[rival_1st_icon_number])
        player_rivals_overlays_vertex_tex(360, 8.5, 192, 103)
        # 2nd Rival Icon
        selected_value = int(ac.getValue(change_rival_2nd_icon_spinner))
        rival_2nd_icon_number = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(player_icon_selected[rival_2nd_icon_number])
        player_rivals_overlays_vertex_tex(360, 120.5, 192, 103)
    # Position Overlay Background For 3 Rivals
    elif cars_in_session >= 4:
        # 1st Rival Icon
        selected_value = int(ac.getValue(change_rival_1st_icon_spinner))
        rival_1st_icon_number = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(player_icon_selected[rival_1st_icon_number])
        player_rivals_overlays_vertex_tex(360, 8.5, 192, 103)
        # 2nd Rival Icon
        selected_value = int(ac.getValue(change_rival_2nd_icon_spinner))
        rival_2nd_icon_number = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(player_icon_selected[rival_2nd_icon_number])
        player_rivals_overlays_vertex_tex(360, 120.5, 192, 103)
        # 3rd Rival Icon
        selected_value = int(ac.getValue(change_rival_3rd_icon_spinner))
        rival_3rd_icon_number = selected_value
        ac.glColor4f(1, 1, 1, 1)
        ac.ext_glSetTexture(player_icon_selected[rival_3rd_icon_number])
        player_rivals_overlays_vertex_tex(360, 233, 192, 103)
    else:
        return

    # Rivals Positions Logic
    if status == 2 and session_type == 2:
        # If in Session No Rivals
        if cars_in_session == 1:
            return
        # If in Session 1 Rival
        elif cars_in_session == 2:
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_1st_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width

            for i, position in enumerate(rivals_1st_overlay_list):
                x = start_x + i * number_width
                y = 14
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)
        
        # If in Session 2 Rivals
        elif cars_in_session == 3:
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_1st_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            
            for i, position in enumerate(rivals_1st_overlay_list):
                x = start_x + i * number_width
                y = 14
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)

            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_2nd_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width

            for i, position in enumerate(rivals_2nd_overlay_list):
                x = start_x + i * number_width
                y = 126
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)

        # If in Session 3 Rivals
        elif cars_in_session >= 4:
            # 1st Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_1st_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            for i, position in enumerate(rivals_1st_overlay_list):
                x = start_x + i * number_width
                y = 14
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)

            # 2nd Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_2nd_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            for i, position in enumerate(rivals_2nd_overlay_list):
                x = start_x + i * number_width
                y = 126
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)

            # 3rd Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_3rd_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            
            for i, position in enumerate(rivals_3rd_overlay_list):
                x = start_x + i * number_width
                y = 238
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)
        else:
            return
    # Same but for Different Session Type
    elif status == 1 and session_type == 2:
        # If in Session No Rivals
        if cars_in_session == 1:
            return
        # If in Session 1 Rival
        elif cars_in_session == 2:
            # 1st Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_1st_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            for i, position in enumerate(rivals_1st_overlay_list):
                x = start_x + i * number_width
                y = 14
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)
        # If in Session 2 Rivals
        elif cars_in_session == 3:
            # 1st Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_1st_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            for i, position in enumerate(rivals_1st_overlay_list):
                x = start_x + i * number_width
                y = 14
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)

            # 2nd Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_2nd_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            for i, position in enumerate(rivals_2nd_overlay_list):
                x = start_x + i * number_width
                y = 126
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)

        # If in Session 3 Rivals
        elif cars_in_session >= 4:
            # 1st Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_1st_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            for i, position in enumerate(rivals_1st_overlay_list):
                x = start_x + i * number_width
                y = 14
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)
            
            # 2nd Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_2nd_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            for i, position in enumerate(rivals_2nd_overlay_list):
                x = start_x + i * number_width
                y = 126
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)

            # 3rd Rival
            ac.glColor4f(*color)
            ac.ext_glSetTexture(position_overlay_numbers)
            number_width = 150
            number_height = 70
            total_numbers = len(rivals_3rd_overlay_list)
            current_width = total_numbers * number_width
            start_x = 150 - current_width
            for i, position in enumerate(rivals_3rd_overlay_list):
                x = start_x + i * number_width
                y = 238
                coord_x1 = int(position) / 9
                coord_x2 = 0.11 + (int(position) / 9)
                player_rivals_overlays_vertex_tex(x, y, number_width, number_height, coord_x1, coord_x2)
        else:
            return
    else:
        return


    ac.glEnd()


def rearviewmirrorGL(detalT):
    
    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # Rear View Mirror Overlay Background Image
    ac.glColor4f(1, 1, 1, 0.15)
    ac.ext_glSetTexture(rear_view_mirror_overlay_background)
    rear_view_mirror_overlay_vertex_tex(0, 0, 700, 210)

    ac.glEnd()


def timecounterGL(detalT):

    ac.ext_glSetCullMode(ExtGL.CULL_MODE_NONE)

    # Time Background
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(time_bg)
    time_vertex_tex(109, 0, 370 / 2.27, 228 / 2.25)

    # Lap Time MS
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(time_numbers)

    time_current_digits_width = 54
    time_current_digits_height = 62
    total_time_current_digits = len(time_current_milliseconds_list)
    time_current__width = total_time_current_digits * time_current_digits_width
    start_x = 430 - time_current__width

    for i, time_current_digits in enumerate(time_current_milliseconds_list):
        x = start_x + i * time_current_digits_width
        y = 0
        coord_x1 = int(time_current_digits) / 10
        coord_x2 = 0.1 + (int(time_current_digits) / 10)
        time_vertex_tex(x, y, time_current_digits_width, time_current_digits_height, coord_x1, coord_x2)

    # Lap Time S
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(time_numbers)

    time_current_digits_width = 54
    time_current_digits_height = 62
    total_time_current_digits = len(time_current_seconds_list)
    time_current_width = total_time_current_digits * time_current_digits_width
    start_x = 243 - time_current_width

    for i, time_current_digits in enumerate(time_current_seconds_list):
        x = start_x + i * time_current_digits_width
        y = 0
        coord_x1 = int(time_current_digits) / 10
        coord_x2 = 0.1 + (int(time_current_digits) / 10)
        time_vertex_tex(x, y, time_current_digits_width, time_current_digits_height, coord_x1, coord_x2)

    # Lap Time M
    ac.glColor4f(1, 1, 1, 1)
    ac.ext_glSetTexture(time_numbers)

    time_current_digits_width = 54
    time_current_digits_height = 62
    total_time_current_digits = len(time_current_minutes_list)
    time_current_width = total_time_current_digits * time_current_digits_width
    start_x = 109 - time_current_width

    for i, time_current_digits in enumerate(time_current_minutes_list):
        x = start_x + i * time_current_digits_width
        y = 0
        coord_x1 = int(time_current_digits) / 10
        coord_x2 = 0.1 + (int(time_current_digits) / 10)
        time_vertex_tex(x, y, time_current_digits_width, time_current_digits_height, coord_x1, coord_x2)


    ac.glEnd()


def acUpdate(deltaT):
    global timer, timer2, gear_delay, status, session_type
    global current_car, state, rpm, maxRpm, gear_current, gear, gear_shift, gear_animation, gear_animation_speed, speed, speed_list, boost, auto_on, handbrake_on
    global player_rivals_overlay_current, player_rivals_overlay_current_list
    global rivals_overlay_list, rivals_1st_overlay, rivals_2nd_overlay, rivals_3rd_overlay, rivals_1st_overlay_list, rivals_2nd_overlay_list, rivals_3rd_overlay_list
    global distance
    global distance_xxdotx_xx, distance_xxdotx_x, distance_xxdotx_xx_list, distance_xxdotx_x_list
    global distance_xdotxx_x, distance_xdotxx_xx, distance_xdotxx_x_list, distance_xdotxx_xx_list
    global time_current, time_current_milliseconds, time_current_seconds, time_current_minutes, time_current_milliseconds_list, time_current_seconds_list, time_current_minutes_list

    # Timers
    timer += deltaT
    timer2 += deltaT

    # Single Time
    if timer > 1:
        timer = 0

        ac.setBackgroundOpacity(config_window, 100)
        ac.setBackgroundOpacity(tachometer_window, 0)
        ac.setBackgroundOpacity(digital_dash_window, 0)
        ac.setBackgroundOpacity(turbo_gauge_window, 0)
        ac.setBackgroundOpacity(map_overlay_window, 0)
        ac.setBackgroundOpacity(player_overlay_window, 0)
        ac.setBackgroundOpacity(rivals_overlay_window, 0)
        ac.setBackgroundOpacity(rear_view_mirror_overlay_window, 0)
        ac.setBackgroundOpacity(time_counter_window, 0)

        status = info.graphics.status
        session_type = info.graphics.session

        gear_animation_speed = info.graphics.replayTimeMultiplier

        if maxRpm == 1:
            maxRpm = info.static.maxRpm

            for i in range(1, len(maxRpm_state)):
                if maxRpm_state[i] <= maxRpm:
                    state = i
                else:
                    if maxRpm_state[0] > maxRpm:
                        state = 0
                    break

    # Gear Code with Animation Delay
    if gear_current == 1:
        gear_delay += deltaT * abs(gear_animation_speed)
        if gear_delay > 0.5: #0.5 Because some transmissions have too much delay in neutral
            gear = gear_current
    else:
        gear_delay = 0

        if gear != gear_current:
            gear_shift = True
            gear = gear_current

        if gear_shift:
            gear_animation += deltaT * 3 * abs(gear_animation_speed)
            if gear_animation > 1:
                gear_shift = False
                gear_animation = 0

    # Fast Time
    if timer2 > 0.01667:
        timer2 = 0

        current_car = ac.getFocusedCar()
        speed = ac.getCarState(current_car, acsys.CS.SpeedKMH)
        if uom_kmh:
            speed = ac.getCarState(current_car, acsys.CS.SpeedKMH)
        else:
            speed = ac.getCarState(current_car, acsys.CS.SpeedMPH)
        speed_list = list("{:.0f}".format(speed))
        rpm = ac.getCarState(current_car, acsys.CS.RPM)
        gear_current = ac.getCarState(current_car, acsys.CS.Gear)
        boost = ac.getCarState(current_car, acsys.CS.TurboBoost)
        auto_on = bool(info.physics.autoShifterOn)
        handbrake_on = bool(ac.ext_getHandbrake(current_car))

        # Distance Generic
        distance = (1 - ac.getCarState(current_car, acsys.CS.NormalizedSplinePosition)) * track_length

        # XX (Thousands) Dot (Space) X (Hundreds)
        distance_xxdotx_xx = int(distance // 1000)
        distance_xxdotx_x = int(distance % 1000) // 100
        distance_xxdotx_xx_str = "{:02.0f}".format(distance_xxdotx_xx)
        distance_xxdotx_x_str = "{:01.0f}".format(distance_xxdotx_x)
        distance_xxdotx_xx_list = list(distance_xxdotx_xx_str)
        distance_xxdotx_x_list = list(distance_xxdotx_x_str)

        # X (Thousands) Dot (Space) XX (Hundres Decades)
        distance_xdotxx_x = int(distance // 1000)
        distance_xdotxx_xx = int(distance % 1000) // 10
        distance_xdotxx_x_str = "{:01.0f}".format(distance_xdotxx_x)
        distance_xdotxx_xx_str = "{:02.0f}".format(distance_xdotxx_xx)
        distance_xdotxx_x_list = list(distance_xdotxx_x_str)
        distance_xdotxx_xx_list = list(distance_xdotxx_xx_str)

        # Lap Time
        time_current = ac.getCarState(current_car, acsys.CS.LapTime)
        time_current_milliseconds = int(time_current % 1000)
        time_current_seconds = int(time_current / 1000) % 60
        time_current_minutes = (time_current // 1000) // 60
        time_current_milliseconds_str = "{:03}".format(time_current_milliseconds)
        time_current_seconds_str = "{:02.0f}".format(time_current_seconds)
        time_current_minutes_str = "{:02.0f}".format(time_current_minutes)
        time_current_milliseconds_list = list(time_current_milliseconds_str)
        time_current_seconds_list = list(time_current_seconds_str)
        time_current_minutes_list = list(time_current_minutes_str)

        # Player Position in Live Race Shows, Live Replay Shows, Replay Replay Hidden
        if session_type == 2:
            if cars_in_session == 1:
                return
            else:
                if session_type == 2:
                    player_rivals_overlay_current = ac.getCarRealTimeLeaderboardPosition(current_car)
                elif status == 1 and session_type == 2:
                    player_rivals_overlay_current = ac.getCarRealTimeLeaderboardPosition(current_car)
                elif status != 1 and session_type == 2:
                    player_rivals_overlay_current = ac.getCarLeaderboardPosition(current_car)

                player_rivals_overlay_current_list = list(str(player_rivals_overlay_current))

        if session_type == 2:
            if cars_in_session == 1:
                return
            elif cars_in_session == 2:
                rivals_1st_overlay = ac.getCarRealTimeLeaderboardPosition(1)
                rivals_1st_overlay_list = list("{:.0f}".format(rivals_1st_overlay))
            elif cars_in_session == 3:
                rivals_1st_overlay = ac.getCarRealTimeLeaderboardPosition(1)
                rivals_1st_overlay_list = list("{:.0f}".format(rivals_1st_overlay))
                rivals_2nd_overlay = ac.getCarRealTimeLeaderboardPosition(2)
                rivals_2nd_overlay_list = list("{:.0f}".format(rivals_2nd_overlay))
            elif cars_in_session >= 4:
                rivals_1st_overlay = ac.getCarRealTimeLeaderboardPosition(1)
                rivals_1st_overlay_list = list("{:.0f}".format(rivals_1st_overlay))
                rivals_2nd_overlay = ac.getCarRealTimeLeaderboardPosition(2)
                rivals_2nd_overlay_list = list("{:.0f}".format(rivals_2nd_overlay))
                rivals_3rd_overlay = ac.getCarRealTimeLeaderboardPosition(3)
                rivals_3rd_overlay_list = list("{:.0f}".format(rivals_3rd_overlay))
        elif status != 1:
            if cars_in_session == 1:
                return
            elif cars_in_session == 2:
                rivals_1st_overlay = ac.getCarLeaderboardPosition(1)
                rivals_1st_overlay_list = list("{:.0f}".format(rivals_1st_overlay))
            elif cars_in_session == 3:
                rivals_1st_overlay = ac.getCarLeaderboardPosition(1)
                rivals_1st_overlay_list = list("{:.0f}".format(rivals_1st_overlay))
                rivals_2nd_overlay = ac.getCarLeaderboardPosition(2)
                rivals_2nd_overlay_list = list("{:.0f}".format(rivals_2nd_overlay))
            elif cars_in_session >= 4:
                rivals_1st_overlay = ac.getCarLeaderboardPosition(1)
                rivals_1st_overlay_list = list("{:.0f}".format(rivals_1st_overlay))
                rivals_2nd_overlay = ac.getCarLeaderboardPosition(2)
                rivals_2nd_overlay_list = list("{:.0f}".format(rivals_2nd_overlay))
                rivals_3rd_overlay = ac.getCarLeaderboardPosition(3)
                rivals_3rd_overlay_list = list("{:.0f}".format(rivals_3rd_overlay))


def config_window_activated(*args):
    global config_window_visibility

    config_window_visibility = 1

def config_window_deactivated(*args):
    global config_window_visibility

    config_window_visibility = 0


def config_scale_spinner_clicked(*args):
    global config, update_config, config_scale

    update_config = True
    config_scale = ac.getValue(config_scale_spinner) / 100

    config.set("WMPS3 HUD", "config_scale", str(ac.getValue(config_scale_spinner)))

def tachometer_scale_spinner_clicked(*args):
    global config, update_config, tachometer_scale

    update_config = True
    tachometer_scale = ac.getValue(tachometer_scale_spinner) / 100

    config.set("WMPS3 HUD", "tachometer_scale", str(ac.getValue(tachometer_scale_spinner)))

def digital_dash_scale_spinner_clicked(*args):
    global config, update_config, digital_dash_scale

    update_config = True
    digital_dash_scale = ac.getValue(digital_dash_scale_spinner) / 100

    config.set("WMPS3 HUD", "digital_dash_scale", str(ac.getValue(digital_dash_scale_spinner)))

def turbo_gauge_scale_spinner_clicked(*args):
    global config, update_config, turbo_gauge_scale

    update_config = True
    turbo_gauge_scale = ac.getValue(turbo_gauge_scale_spinner) / 100

    config.set("WMPS3 HUD", "turbo_gauge_scale", str(ac.getValue(turbo_gauge_scale_spinner)))

def map_overlay_scale_spinner_clicked(*args):
    global config, update_config, map_overlay_scale

    update_config = True
    map_overlay_scale = ac.getValue(map_overlay_scale_spinner) / 100

    config.set("WMPS3 HUD", "map_overlay_scale", str(ac.getValue(map_overlay_scale_spinner)))

def player_rivals_overlays_scale_spinner_clicked(*args):
    global config, update_config, player_rivals_overlays_scale

    update_config = True
    player_rivals_overlays_scale = ac.getValue(player_rivals_overlays_scale_spinner) / 100

    config.set("WMPS3 HUD", "player_rivals_overlays_scale", str(ac.getValue(player_rivals_overlays_scale_spinner)))

def rear_view_mirror_overlay_scale_spinner_clicked(*args):
    global config, update_config, rear_view_mirror_overlay_scale

    update_config = True
    rear_view_mirror_overlay_scale = ac.getValue(rear_view_mirror_overlay_scale_spinner) / 100

    config.set("WMPS3 HUD", "rear_view_mirror_overlay_scale", str(ac.getValue(rear_view_mirror_overlay_scale_spinner)))

def time_scale_spinner_clicked(*args):
    global config, update_config, time_scale

    update_config = True
    time_scale = ac.getValue(time_scale_spinner) / 100

    config.set("WMPS3 HUD", "time_scale", str(ac.getValue(time_scale_spinner)))


def tachometer_background_opacity_spinner_clicked(*args):
    global config, update_config, tachometer_background_opacity_index
    
    update_config = True
    tachometer_background_opacity_index = int(ac.getValue(tachometer_background_opacity_spinner))

    config.set("WMPS3 HUD", "tachometer_background_opacity_index", str(tachometer_background_opacity_index))

def turbo_gauge_background_opacity_spinner_clicked(*args):
    global config, update_config, turbo_gauge_background_opacity_index
    
    update_config = True
    turbo_gauge_background_opacity_index = int(ac.getValue(turbo_gauge_background_opacity_spinner))

    config.set("WMPS3 HUD", "turbo_gauge_background_opacity_index", str(turbo_gauge_background_opacity_index))


def change_player_icon_spinner_clicked(*args):
    global config, update_config, player_icon_number  

    update_config = True
    player_icon_number = int(ac.getValue(change_player_icon_spinner))

    config.set("WMPS3 HUD", "player_icon_number", str(player_icon_number))

def change_rival_1st_icon_spinner_clicked(*args):
    global config, update_config, rival_1st_icon_number

    update_config = True
    rival_1st_icon_number = int(ac.getValue(change_rival_1st_icon_spinner))

    config.set("WMPS3 HUD", "rival_1st_icon_number", str(rival_1st_icon_number))

def change_rival_2nd_icon_spinner_clicked(*args):
    global config, update_config, rival_2nd_icon_number

    update_config = True
    rival_2nd_icon_number = int(ac.getValue(change_rival_2nd_icon_spinner))

    config.set("WMPS3 HUD", "rival_2nd_icon_number", str(rival_2nd_icon_number))

def change_rival_3rd_icon_spinner_clicked(*args):
    global config, update_config, rival_3rd_icon_number

    update_config = True
    rival_3rd_icon_number = int(ac.getValue(change_rival_3rd_icon_spinner))

    config.set("WMPS3 HUD", "rival_3rd_icon_number", str(rival_3rd_icon_number))


def player_overlay_background_color_spinner_clicked(*args):
    global config, update_config, player_overlay_background_color_index
    
    update_config = True
    player_overlay_background_color_index = int(ac.getValue(player_overlay_background_color_spinner))

    config.set("WMPS3 HUD", "player_overlay_background_color_index", str(player_overlay_background_color_index))

def rival_1st_overlay_background_color_spinner_clicked(*args):
    global config, update_config, rival_1st_overlay_background_color_index
    
    update_config = True
    rival_1st_overlay_background_color_index = int(ac.getValue(rival_1st_overlay_background_color_spinner))

    config.set("WMPS3 HUD", "rival_1st_overlay_background_color_index", str(rival_1st_overlay_background_color_index))

def rival_2nd_overlay_background_color_spinner_clicked(*args):
    global config, update_config, rival_2nd_overlay_background_color_index
    
    update_config = True
    rival_2nd_overlay_background_color_index = int(ac.getValue(rival_2nd_overlay_background_color_spinner))

    config.set("WMPS3 HUD", "rival_2nd_overlay_background_color_index", str(rival_2nd_overlay_background_color_index))

def rival_3rd_overlay_background_color_spinner_clicked(*args):
    global config, update_config, rival_3rd_overlay_background_color_index
    
    update_config = True
    rival_3rd_overlay_background_color_index = int(ac.getValue(rival_3rd_overlay_background_color_spinner))

    config.set("WMPS3 HUD", "rival_3rd_overlay_background_color_index", str(rival_3rd_overlay_background_color_index))


def change_player_type_spinner_clicked(*args):
    global config, update_config, player_type

    update_config = True
    player_type = int(ac.getValue(change_player_type_spinner))

    config.set("WMPS3 HUD", "player_type", str(player_type))

def change_rival_1st_type_spinner_clicked(*args):
    global config, update_config, rival_1st_type

    update_config = True
    rival_1st_type = int(ac.getValue(change_rival_1st_type_spinner))

    config.set("WMPS3 HUD", "rival_1st_type", str(rival_1st_type))

def change_rival_2nd_type_spinner_clicked(*args):
    global config, update_config, rival_2nd_type

    update_config = True
    rival_2nd_type = int(ac.getValue(change_rival_2nd_type_spinner))

    config.set("WMPS3 HUD", "rival_2nd_type", str(rival_2nd_type))

def change_rival_3rd_type_spinner_clicked(*args):
    global config, update_config, rival_3rd_type

    update_config = True
    rival_3rd_type = int(ac.getValue(change_rival_3rd_type_spinner))

    config.set("WMPS3 HUD", "rival_3rd_type", str(rival_3rd_type))


def player_rivals_type_position_spinner_clicked(*args):
    global config, update_config, player_rivals_type_position_index
    
    update_config = True
    player_rivals_type_position_index = int(ac.getValue(player_rivals_type_position_spinner))

    config.set("WMPS3 HUD", "player_rivals_type_position_index", str(player_rivals_type_position_index))

def player_mode_type_spinner_clicked(*args):
    global config, update_config, player_mode_type

    update_config = True
    player_mode_type = int(ac.getValue(player_mode_type_spinner))

    config.set("WMPS3 HUD", "player_mode_type", str(player_mode_type))

def player_mode_position_spinner_clicked(*args):
    global config, update_config, mode_type_position_index
    
    update_config = True
    mode_type_position_index = int(ac.getValue(player_mode_position_spinner))

    config.set("WMPS3 HUD", "mode_type_position_index", str(mode_type_position_index))

def show_player_rivals_current_positions_checkbox_clicked(*args):
    global config, update_config, show_player_rivals_current_positions

    update_config = True

    if show_player_rivals_current_positions:
        show_player_rivals_current_positions = False
    else:
        show_player_rivals_current_positions = True

    config.set("WMPS3 HUD", "show_player_rivals_current_positions", str(show_player_rivals_current_positions))

def show_map_overlay_text_checkbox_clicked(*args):
    global config, update_config, show_map_overlay_text

    update_config = True

    if show_map_overlay_text:
        show_map_overlay_text = False
    else:
        show_map_overlay_text = True

    config.set("WMPS3 HUD", "show_map_overlay_text", str(show_map_overlay_text))


def resolution_720p_button_clicked(*args):
    global config, update_config, config_scale, tachometer_scale, digital_dash_scale, turbo_gauge_scale, map_overlay_scale, player_rivals_overlays_scale, rear_view_mirror_overlay_scale, time_scale

    update_config = True

    config.set("WMPS3 HUD", "config_scale", "37")
    ac.setValue(config_scale_spinner, 37)

    config.set("WMPS3 HUD", "tachometer_scale", "31.5")
    ac.setValue(tachometer_scale_spinner, 31.5)

    config.set("WMPS3 HUD", "digital_dash_scale", "33")
    ac.setValue(digital_dash_scale_spinner, 33)

    config.set("WMPS3 HUD", "turbo_gauge_scale", "35")
    ac.setValue(turbo_gauge_scale_spinner, 35)

    config.set("WMPS3 HUD", "map_overlay_scale", "67")
    ac.setValue(map_overlay_scale_spinner, 67)

    config.set("WMPS3 HUD", "player_rivals_overlays_scale", "66")
    ac.setValue(player_rivals_overlays_scale_spinner, 66)

    config.set("WMPS3 HUD", "rear_view_mirror_overlay_scale", "52")
    ac.setValue(rear_view_mirror_overlay_scale_spinner, 52)

    config.set("WMPS3 HUD", "time_scale", "50")
    ac.setValue(time_scale_spinner, 50)

    ac.setPosition(config_window, default_config_position[0] * config_scale, default_config_position[1] * config_scale - 5)
    ac.setPosition(tachometer_window, default_tachometer_position[0] * tachometer_scale + 18, default_tachometer_position[1] * tachometer_scale + 7.5)
    ac.setPosition(digital_dash_window, default_digital_dash_position[0] * digital_dash_scale - 1, default_digital_dash_position[1] * digital_dash_scale - 1)
    ac.setPosition(turbo_gauge_window, default_turbo_gauge_position[0] * turbo_gauge_scale, default_turbo_gauge_position[1] * turbo_gauge_scale + 1)
    ac.setPosition(map_overlay_window, default_map_position[0] * map_overlay_scale - 2, default_map_position[1] * map_overlay_scale - 5)
    ac.setPosition(player_overlay_window, default_position_player_overlay[0] * player_rivals_overlays_scale + 1, default_position_player_overlay[1] * player_rivals_overlays_scale + 1)
    ac.setPosition(rivals_overlay_window, default_position_rivals_overlay[0] * player_rivals_overlays_scale + 7, default_position_rivals_overlay[1] * player_rivals_overlays_scale + 1)
    ac.setPosition(rear_view_mirror_overlay_window, default_rear_view_mirror_overlay[0] * rear_view_mirror_overlay_scale, default_rear_view_mirror_overlay[1] * rear_view_mirror_overlay_scale + 42.5)
    ac.setPosition(time_counter_window, default_time_position[0] * time_scale, default_time_position[1] * time_scale)


def resolution_1080p_button_clicked(*args):
    global config, update_config, config_scale, tachometer_scale, digital_dash_scale, turbo_gauge_scale, map_overlay_scale, player_rivals_overlays_scale, rear_view_mirror_overlay_scale, time_scale

    update_config = True

    config.set("WMPS3 HUD", "config_scale", "55")
    ac.setValue(config_scale_spinner, 55)

    config.set("WMPS3 HUD", "tachometer_scale", "47")
    ac.setValue(tachometer_scale_spinner, 47)

    config.set("WMPS3 HUD", "digital_dash_scale", "50")
    ac.setValue(digital_dash_scale_spinner, 50)

    config.set("WMPS3 HUD", "turbo_gauge_scale", "52.5")
    ac.setValue(turbo_gauge_scale_spinner, 52.5)

    config.set("WMPS3 HUD", "map_overlay_scale", "100")
    ac.setValue(map_overlay_scale_spinner, 100)

    config.set("WMPS3 HUD", "player_rivals_overlays_scale", "100")
    ac.setValue(player_rivals_overlays_scale_spinner, 100)

    config.set("WMPS3 HUD", "rear_view_mirror_overlay_scale", "78")
    ac.setValue(rear_view_mirror_overlay_scale_spinner, 78)

    config.set("WMPS3 HUD", "time_scale", "75")
    ac.setValue(time_scale_spinner, 75)


    ac.setPosition(config_window, default_config_position[0] * config_scale, default_config_position[1] * config_scale - 20)
    ac.setPosition(tachometer_window, default_tachometer_position[0] * tachometer_scale + 9, default_tachometer_position[1] * tachometer_scale + 3)
    ac.setPosition(digital_dash_window, default_digital_dash_position[0] * digital_dash_scale - 16, default_digital_dash_position[1] * digital_dash_scale - 12)
    ac.setPosition(turbo_gauge_window, default_turbo_gauge_position[0] * turbo_gauge_scale + 17, default_turbo_gauge_position[1] * turbo_gauge_scale + 5)
    ac.setPosition(map_overlay_window, default_map_position[0] * map_overlay_scale - 1, default_map_position[1] * map_overlay_scale - 3)
    ac.setPosition(player_overlay_window, default_position_player_overlay[0] * player_rivals_overlays_scale - 1, default_position_player_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rivals_overlay_window, default_position_rivals_overlay[0] * player_rivals_overlays_scale - 6, default_position_rivals_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rear_view_mirror_overlay_window, default_rear_view_mirror_overlay[0] * rear_view_mirror_overlay_scale, default_rear_view_mirror_overlay[1] * rear_view_mirror_overlay_scale + 21)
    ac.setPosition(time_counter_window, default_time_position[0] * time_scale, default_time_position[1] * time_scale)

def resolution_1080p_uw_button_clicked(*args):
    global config, update_config, config_scale, tachometer_scale, digital_dash_scale, turbo_gauge_scale, map_overlay_scale, player_rivals_overlays_scale, rear_view_mirror_overlay_scale, time_scale

    update_config = True

    config.set("WMPS3 HUD", "config_scale", "55")
    ac.setValue(config_scale_spinner, 55)

    config.set("WMPS3 HUD", "tachometer_scale", "47")
    ac.setValue(tachometer_scale_spinner, 47)

    config.set("WMPS3 HUD", "digital_dash_scale", "50")
    ac.setValue(digital_dash_scale_spinner, 50)

    config.set("WMPS3 HUD", "turbo_gauge_scale", "52.5")
    ac.setValue(turbo_gauge_scale_spinner, 52.5)

    config.set("WMPS3 HUD", "map_overlay_scale", "100")
    ac.setValue(map_overlay_scale_spinner, 100)

    config.set("WMPS3 HUD", "player_rivals_overlays_scale", "100")
    ac.setValue(player_rivals_overlays_scale_spinner, 100)

    config.set("WMPS3 HUD", "rear_view_mirror_overlay_scale", "78")
    ac.setValue(rear_view_mirror_overlay_scale_spinner, 78)

    config.set("WMPS3 HUD", "time_scale", "75")
    ac.setValue(time_scale_spinner, 75)

    ac.setPosition(config_window, default_config_position[0] * config_scale, default_config_position[1] * config_scale - 20)
    ac.setPosition(tachometer_window, default_tachometer_position[0] * tachometer_scale + 648, default_tachometer_position[1] * tachometer_scale + 3)
    ac.setPosition(digital_dash_window, default_digital_dash_position[0] * digital_dash_scale + 622, default_digital_dash_position[1] * digital_dash_scale - 10)
    ac.setPosition(turbo_gauge_window, default_turbo_gauge_position[0] * turbo_gauge_scale + 657, default_turbo_gauge_position[1] * turbo_gauge_scale + 5)
    ac.setPosition(map_overlay_window, default_map_position[0] * map_overlay_scale - 1, default_map_position[1] * map_overlay_scale - 3)
    ac.setPosition(player_overlay_window, default_position_player_overlay[0] * player_rivals_overlays_scale - 1, default_position_player_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rivals_overlay_window, default_position_rivals_overlay[0] * player_rivals_overlays_scale + 633, default_position_rivals_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rear_view_mirror_overlay_window, default_rear_view_mirror_overlay[0] * rear_view_mirror_overlay_scale + 320, default_rear_view_mirror_overlay[1] * rear_view_mirror_overlay_scale + 21)
    ac.setPosition(time_counter_window, default_time_position[0] * time_scale + 319, default_time_position[1] * time_scale)

def resolution_1440p_button_clicked(*args):
    global config, update_config, config_scale, tachometer_scale, digital_dash_scale, turbo_gauge_scale, map_overlay_scale, player_rivals_overlays_scale, rear_view_mirror_overlay_scale, time_scale

    update_config = True

    config.set("WMPS3 HUD", "config_scale", "73")
    ac.setValue(config_scale_spinner, 73)

    config.set("WMPS3 HUD", "tachometer_scale", "63")
    ac.setValue(tachometer_scale_spinner, 63)

    config.set("WMPS3 HUD", "digital_dash_scale", "66")
    ac.setValue(digital_dash_scale_spinner, 66)

    config.set("WMPS3 HUD", "turbo_gauge_scale", "70")
    ac.setValue(turbo_gauge_scale_spinner, 70)

    config.set("WMPS3 HUD", "map_overlay_scale", "133")
    ac.setValue(map_overlay_scale_spinner, 133)

    config.set("WMPS3 HUD", "player_rivals_overlays_scale", "133")
    ac.setValue(player_rivals_overlays_scale_spinner, 133)

    config.set("WMPS3 HUD", "rear_view_mirror_overlay_scale", "104")
    ac.setValue(rear_view_mirror_overlay_scale_spinner, 104)

    config.set("WMPS3 HUD", "time_scale", "100")
    ac.setValue(time_scale_spinner, 100)

    ac.setPosition(config_window, default_config_position[0] * config_scale, default_config_position[1] * config_scale - 50)
    ac.setPosition(tachometer_window, default_tachometer_position[0] * tachometer_scale, default_tachometer_position[1] * tachometer_scale)
    ac.setPosition(digital_dash_window, default_digital_dash_position[0] * digital_dash_scale, default_digital_dash_position[1] * digital_dash_scale)
    ac.setPosition(turbo_gauge_window, default_turbo_gauge_position[0] * turbo_gauge_scale, default_turbo_gauge_position[1] * turbo_gauge_scale)
    ac.setPosition(map_overlay_window, default_map_position[0] * map_overlay_scale, default_map_position[1] * map_overlay_scale)
    ac.setPosition(player_overlay_window, default_position_player_overlay[0] * player_rivals_overlays_scale, default_position_player_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rivals_overlay_window, default_position_rivals_overlay[0] * player_rivals_overlays_scale, default_position_rivals_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rear_view_mirror_overlay_window, default_rear_view_mirror_overlay[0] * rear_view_mirror_overlay_scale, default_rear_view_mirror_overlay[1] * rear_view_mirror_overlay_scale)
    ac.setPosition(time_counter_window, default_time_position[0] * time_scale, default_time_position[1] * time_scale)

def resolution_1440p_uw_button_clicked(*args):
    global config, update_config, config_scale, tachometer_scale, digital_dash_scale, turbo_gauge_scale, map_overlay_scale, player_rivals_overlays_scale, rear_view_mirror_overlay_scale, time_scale

    update_config = True

    config.set("WMPS3 HUD", "config_scale", "73")
    ac.setValue(config_scale_spinner, 73)

    config.set("WMPS3 HUD", "tachometer_scale", "63")
    ac.setValue(tachometer_scale_spinner, 63)

    config.set("WMPS3 HUD", "digital_dash_scale", "66")
    ac.setValue(digital_dash_scale_spinner, 66)

    config.set("WMPS3 HUD", "turbo_gauge_scale", "70")
    ac.setValue(turbo_gauge_scale_spinner, 70)

    config.set("WMPS3 HUD", "map_overlay_scale", "133")
    ac.setValue(map_overlay_scale_spinner, 133)

    config.set("WMPS3 HUD", "player_rivals_overlays_scale", "133")
    ac.setValue(player_rivals_overlays_scale_spinner, 133)

    config.set("WMPS3 HUD", "rear_view_mirror_overlay_scale", "104")
    ac.setValue(rear_view_mirror_overlay_scale_spinner, 104)

    config.set("WMPS3 HUD", "time_scale", "100")
    ac.setValue(time_scale_spinner, 100)

    ac.setPosition(config_window, default_config_position[0] * config_scale, default_config_position[1] * config_scale - 50)
    ac.setPosition(tachometer_window, default_tachometer_position[0] * tachometer_scale + 878, default_tachometer_position[1] * tachometer_scale - 1)
    ac.setPosition(digital_dash_window, default_digital_dash_position[0] * digital_dash_scale + 875, default_digital_dash_position[1] * digital_dash_scale - 1)
    ac.setPosition(turbo_gauge_window, default_turbo_gauge_position[0] * turbo_gauge_scale + 878, default_turbo_gauge_position[1] * turbo_gauge_scale + 1)
    ac.setPosition(map_overlay_window, default_map_position[0] * map_overlay_scale + 1, default_map_position[1] * map_overlay_scale - 1)
    ac.setPosition(player_overlay_window, default_position_player_overlay[0] * player_rivals_overlays_scale, default_position_player_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rivals_overlay_window, default_position_rivals_overlay[0] * player_rivals_overlays_scale + 878, default_position_rivals_overlay[1] * player_rivals_overlays_scale + 1)
    ac.setPosition(rear_view_mirror_overlay_window, default_rear_view_mirror_overlay[0] * rear_view_mirror_overlay_scale + 440, default_rear_view_mirror_overlay[1] * rear_view_mirror_overlay_scale)
    ac.setPosition(time_counter_window, default_time_position[0] * time_scale + 440, default_time_position[1] * time_scale)

def resolution_4k_button_clicked(*args):
    global config, update_config, config_scale, tachometer_scale, digital_dash_scale, turbo_gauge_scale, map_overlay_scale, player_rivals_overlays_scale, rear_view_mirror_overlay_scale, time_scale

    update_config = True

    config.set("WMPS3 HUD", "config_scale", "100")
    ac.setValue(config_scale_spinner, 100)

    config.set("WMPS3 HUD", "tachometer_scale", "94")
    ac.setValue(tachometer_scale_spinner, 94)

    config.set("WMPS3 HUD", "digital_dash_scale", "99")
    ac.setValue(digital_dash_scale_spinner, 99)

    config.set("WMPS3 HUD", "turbo_gauge_scale", "105")
    ac.setValue(turbo_gauge_scale_spinner, 105)

    config.set("WMPS3 HUD", "map_overlay_scale", "200")
    ac.setValue(map_overlay_scale_spinner, 200)

    config.set("WMPS3 HUD", "player_rivals_overlays_scale", "200")
    ac.setValue(player_rivals_overlays_scale_spinner, 200)

    config.set("WMPS3 HUD", "rear_view_mirror_overlay_scale", "156")
    ac.setValue(rear_view_mirror_overlay_scale_spinner, 156)

    config.set("WMPS3 HUD", "time_scale", "150")
    ac.setValue(time_scale_spinner, 150)

    ac.setPosition(config_window, default_config_position[0] * config_scale, default_config_position[1] * config_scale)
    ac.setPosition(tachometer_window, default_tachometer_position[0] * tachometer_scale + 17, default_tachometer_position[1] * tachometer_scale + 7)
    ac.setPosition(digital_dash_window, default_digital_dash_position[0] * digital_dash_scale - 3, default_digital_dash_position[1] * digital_dash_scale)
    ac.setPosition(turbo_gauge_window, default_turbo_gauge_position[0] * turbo_gauge_scale, default_turbo_gauge_position[1] * turbo_gauge_scale)
    ac.setPosition(map_overlay_window, default_map_position[0] * map_overlay_scale - 1, default_map_position[1] * map_overlay_scale - 6)
    ac.setPosition(player_overlay_window, default_position_player_overlay[0] * player_rivals_overlays_scale - 1, default_position_player_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rivals_overlay_window, default_position_rivals_overlay[0] * player_rivals_overlays_scale - 13, default_position_rivals_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rear_view_mirror_overlay_window, default_rear_view_mirror_overlay[0] * rear_view_mirror_overlay_scale, default_rear_view_mirror_overlay[1] * rear_view_mirror_overlay_scale - 45)
    ac.setPosition(time_counter_window, default_time_position[0] * time_scale - 1, default_time_position[1] * time_scale)

def reset_all_button_clicked(*args):
    global config, update_config, config_scale, tachometer_scale, digital_dash_scale, turbo_gauge_scale, map_overlay_scale, player_rivals_overlays_scale, rear_view_mirror_overlay_scale, time_scale
    global player_icon_number, rival_1st_icon_number, rival_2nd_icon_number,rival_3rd_icon_number
    global player_overlay_background_color_index, rival_1st_overlay_background_color_index, rival_2nd_overlay_background_color_index, rival_3rd_overlay_background_color_index
    global player_type, rival_1st_type, rival_2nd_type, rival_3rd_type
    global tachometer_background_opacity_index, turbo_gauge_background_opacity_index
    global player_rivals_type_position_index, player_mode_type, mode_type_position_index
    global show_player_rivals_current_positions, show_map_overlay_text

    update_config = True

    config.set("WMPS3 HUD", "config_scale", "55")
    ac.setValue(config_scale_spinner, 55)

    config.set("WMPS3 HUD", "tachometer_scale", "47")
    ac.setValue(tachometer_scale_spinner, 47)

    config.set("WMPS3 HUD", "digital_dash_scale", "50")
    ac.setValue(digital_dash_scale_spinner, 50)

    config.set("WMPS3 HUD", "turbo_gauge_scale", "52.5")
    ac.setValue(turbo_gauge_scale_spinner, 52.5)

    config.set("WMPS3 HUD", "map_overlay_scale", "100")
    ac.setValue(map_overlay_scale_spinner, 100)

    config.set("WMPS3 HUD", "player_rivals_overlays_scale", "100")
    ac.setValue(player_rivals_overlays_scale_spinner, 100)

    config.set("WMPS3 HUD", "rear_view_mirror_overlay_scale", "78")
    ac.setValue(rear_view_mirror_overlay_scale_spinner, 78)

    config.set("WMPS3 HUD", "time_scale", "75")
    ac.setValue(time_scale_spinner, 75)

    config.set("WMPS3 HUD", "player_icon_number", "2")
    ac.setValue(change_player_icon_spinner, 2)

    config.set("WMPS3 HUD", "rival_1st_icon_number", "4")
    ac.setValue(change_rival_1st_icon_spinner, 4)
    
    config.set("WMPS3 HUD", "rival_2nd_icon_number", "6")
    ac.setValue(change_rival_2nd_icon_spinner, 6)

    config.set("WMPS3 HUD", "rival_3rd_icon_number", "8")
    ac.setValue(change_rival_3rd_icon_spinner, 8)

    config.set("WMPS3 HUD", "player_overlay_background_color_index", "0")
    ac.setValue(player_overlay_background_color_spinner, 0)

    config.set("WMPS3 HUD", "rival_1st_overlay_background_color_index", "0")
    ac.setValue(rival_1st_overlay_background_color_spinner, 0)
    
    config.set("WMPS3 HUD", "rival_2nd_overlay_background_color_index", "0")
    ac.setValue(rival_2nd_overlay_background_color_spinner, 0)

    config.set("WMPS3 HUD", "rival_3rd_overlay_background_color_index", "0")
    ac.setValue(rival_3rd_overlay_background_color_spinner, 0)

    config.set("WMPS3 HUD", "player_type", "7")
    ac.setValue(change_player_type_spinner, 7)

    config.set("WMPS3 HUD", "rival_1st_type", "12")
    ac.setValue(change_rival_1st_type_spinner, 12)
    
    config.set("WMPS3 HUD", "rival_2nd_type", "13")
    ac.setValue(change_rival_2nd_type_spinner, 13)

    config.set("WMPS3 HUD", "rival_3rd_type", "14")
    ac.setValue(change_rival_3rd_type_spinner, 14)

    config.set("WMPS3 HUD", "tachometer_background_opacity_index", "10")
    ac.setValue(tachometer_background_opacity_spinner, 10)

    config.set("WMPS3 HUD", "turbo_gauge_background_opacity_index", "10")
    ac.setValue(turbo_gauge_background_opacity_spinner, 10)

    config.set("WMPS3 HUD", "player_rivals_type_position_index", "1")
    ac.setValue(player_mode_position_spinner, 1)

    config.set("WMPS3 HUD", "player_mode_type", "0")
    ac.setValue(player_mode_type_spinner, 0)

    config.set("WMPS3 HUD", "mode_type_position_index", "0")
    ac.setValue(player_mode_position_spinner, 0)

    config.set("WMPS3 HUD", "show_player_rivals_current_positions", "True")
    show_player_rivals_current_positions = True

    config.set("WMPS3 HUD", "show_map_overlay_text", "True")
    show_map_overlay_text = True

    ac.setPosition(config_window, default_config_position[0] * config_scale, default_config_position[1] * config_scale - 20)
    ac.setPosition(tachometer_window, default_tachometer_position[0] * tachometer_scale + 9, default_tachometer_position[1] * tachometer_scale + 3)
    ac.setPosition(digital_dash_window, default_digital_dash_position[0] * digital_dash_scale - 16, default_digital_dash_position[1] * digital_dash_scale - 12)
    ac.setPosition(turbo_gauge_window, default_turbo_gauge_position[0] * turbo_gauge_scale + 17, default_turbo_gauge_position[1] * turbo_gauge_scale + 5)
    ac.setPosition(map_overlay_window, default_map_position[0] * map_overlay_scale - 1, default_map_position[1] * map_overlay_scale - 3)
    ac.setPosition(player_overlay_window, default_position_player_overlay[0] * player_rivals_overlays_scale - 1, default_position_player_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rivals_overlay_window, default_position_rivals_overlay[0] * player_rivals_overlays_scale - 6, default_position_rivals_overlay[1] * player_rivals_overlays_scale)
    ac.setPosition(rear_view_mirror_overlay_window, default_rear_view_mirror_overlay[0] * rear_view_mirror_overlay_scale, default_rear_view_mirror_overlay[1] * rear_view_mirror_overlay_scale + 21)
    ac.setPosition(time_counter_window, default_time_position[0] * time_scale, default_time_position[1] * time_scale)


def acShutdown():
    global config, update_config, config_path
    
    if update_config:
        with open(config_path, 'w') as file_config:
            config.write(file_config)


def ext_acDispose():
    global config, update_config, config_path

    if update_config:
        with open(config_path, 'w') as file_config:
            config.write(file_config)


def config_vertex_tex(x, y, width, height, coord_x1 = 0, coord_x2 = 1, coord_y1 = 0, coord_y2 = 1):
    ac.glBegin(acsys.GL.Quads)
    ac.ext_glVertexTex(x * config_scale, y * config_scale, coord_x1, coord_y1)
    ac.ext_glVertexTex(x * config_scale, (y + height) * config_scale, coord_x1, coord_y2)
    ac.ext_glVertexTex((x + width) * config_scale, (y + height) * config_scale, coord_x2, coord_y2)
    ac.ext_glVertexTex((x + width) * config_scale, y * config_scale, coord_x2, coord_y1)
    ac.glEnd()


def tachometer_vertex_tex(x, y, width, height, coord_x1 = 0, coord_x2 = 1, coord_y1 = 0, coord_y2 = 1):
    ac.glBegin(acsys.GL.Quads)
    ac.ext_glVertexTex(x * tachometer_scale, y * tachometer_scale, coord_x1, coord_y1)
    ac.ext_glVertexTex(x * tachometer_scale, (y + height) * tachometer_scale, coord_x1, coord_y2)
    ac.ext_glVertexTex((x + width) * tachometer_scale, (y + height) * tachometer_scale, coord_x2, coord_y2)
    ac.ext_glVertexTex((x + width) * tachometer_scale, y * tachometer_scale, coord_x2, coord_y1)
    ac.glEnd()


def tachometer_needle_rotate(x, y, width, height, center_x, center_y, degree, coord_x1 = 0, coord_x2 = 1, coord_y1 = 0, coord_y2 = 1):
    radians = math.radians(degree)
    cos_degree = math.cos(radians)
    sin_degree = math.sin(radians)

    ac.glBegin(acsys.GL.Quads)

    ac.ext_glVertexTex((center_x + (x - center_x) * cos_degree - (y - center_y) * sin_degree) * tachometer_scale,
                       (center_y + (x - center_x) * sin_degree + (y - center_y) * cos_degree) * tachometer_scale, coord_x1, coord_y1)

    ac.ext_glVertexTex((center_x + (x - center_x) * cos_degree - (y + height - center_y) * sin_degree) * tachometer_scale,
                       (center_y + (x - center_x) * sin_degree + (y + height - center_y) * cos_degree) * tachometer_scale, coord_x1, coord_y2)

    ac.ext_glVertexTex((center_x + (x + width - center_x) * cos_degree - (y + height - center_y) * sin_degree) * tachometer_scale,
                       (center_y + (x + width - center_x) * sin_degree + (y + height - center_y) * cos_degree) * tachometer_scale, coord_x2, coord_y2)

    ac.ext_glVertexTex((center_x + (x + width - center_x) * cos_degree - (y - center_y) * sin_degree) * tachometer_scale,
                       (center_y + (x + width - center_x) * sin_degree + (y - center_y) * cos_degree) * tachometer_scale, coord_x2, coord_y1)

    ac.glEnd()


def digital_dash_vertex_tex(x, y, width, height, coord_x1 = 0, coord_x2 = 1, coord_y1 = 0, coord_y2 = 1):
    ac.glBegin(acsys.GL.Quads)
    ac.ext_glVertexTex(x * digital_dash_scale, y * digital_dash_scale, coord_x1, coord_y1)
    ac.ext_glVertexTex(x * digital_dash_scale, (y + height) * digital_dash_scale, coord_x1, coord_y2)
    ac.ext_glVertexTex((x + width) * digital_dash_scale, (y + height) * digital_dash_scale, coord_x2, coord_y2)
    ac.ext_glVertexTex((x + width) * digital_dash_scale, y * digital_dash_scale, coord_x2, coord_y1)
    ac.glEnd()


def turbo_gauge_vertex_tex(x, y, width, height, coord_x1 = 0, coord_x2 = 1, coord_y1 = 0, coord_y2 = 1):
    ac.glBegin(acsys.GL.Quads)
    ac.ext_glVertexTex(x * turbo_gauge_scale, y * turbo_gauge_scale, coord_x1, coord_y1)
    ac.ext_glVertexTex(x * turbo_gauge_scale, (y + height) * turbo_gauge_scale, coord_x1, coord_y2)
    ac.ext_glVertexTex((x + width) * turbo_gauge_scale, (y + height) * turbo_gauge_scale, coord_x2, coord_y2)
    ac.ext_glVertexTex((x + width) * turbo_gauge_scale, y * turbo_gauge_scale, coord_x2, coord_y1)
    ac.glEnd()


def turbo_gauge_rotate(x, y, width, height, center_x, center_y, spin_rate, degree_offset):

    ac.glBegin(acsys.GL.Quads)

    ac.ext_glTexCoord2f(0, 0)
    ac.glVertex2f((center_x + (x - center_x) * math.cos(math.radians(boost * spin_rate + degree_offset)) - (y - center_y) * math.sin(math.radians(boost * spin_rate + degree_offset))) * turbo_gauge_scale,
                  (center_y + (x - center_x) * math.sin(math.radians(boost * spin_rate + degree_offset)) + (y - center_y) * math.cos(math.radians(boost * spin_rate + degree_offset))) * turbo_gauge_scale)

    ac.ext_glTexCoord2f(1, 0)
    ac.glVertex2f((center_x + (x + width - center_x) * math.cos(math.radians(boost * spin_rate + degree_offset)) - (y - center_y) * math.sin(math.radians(boost * spin_rate + degree_offset))) * turbo_gauge_scale,
                  (center_y + (x + width - center_x) * math.sin(math.radians(boost * spin_rate + degree_offset)) + (y - center_y) * math.cos(math.radians(boost * spin_rate + degree_offset))) * turbo_gauge_scale)

    ac.ext_glTexCoord2f(1, 1)
    ac.glVertex2f((center_x + (x + width - center_x) * math.cos(math.radians(boost * spin_rate + degree_offset)) - (y + height - center_y) * math.sin(math.radians(boost * spin_rate + degree_offset))) * turbo_gauge_scale,
                  (center_y + (x + width - center_x) * math.sin(math.radians(boost * spin_rate + degree_offset)) + (y + height - center_y) * math.cos(math.radians(boost * spin_rate + degree_offset))) * turbo_gauge_scale)

    ac.ext_glTexCoord2f(0, 1)
    ac.glVertex2f((center_x + (x - center_x) * math.cos(math.radians(boost * spin_rate + degree_offset)) - (y + height - center_y) * math.sin(math.radians(boost * spin_rate + degree_offset))) * turbo_gauge_scale,
                  (center_y + (x - center_x) * math.sin(math.radians(boost * spin_rate + degree_offset)) + (y + height - center_y) * math.cos(math.radians(boost * spin_rate + degree_offset))) * turbo_gauge_scale)

    ac.glEnd()


def map_overlay_vertex_tex(x, y, width, height, coord_x1 = 0, coord_x2 = 1, coord_y1 = 0, coord_y2 = 1):
    ac.glBegin(acsys.GL.Quads)
    ac.ext_glVertexTex(x * map_overlay_scale, y * map_overlay_scale, coord_x1, coord_y1)
    ac.ext_glVertexTex(x * map_overlay_scale, (y + height) * map_overlay_scale, coord_x1, coord_y2)
    ac.ext_glVertexTex((x + width) * map_overlay_scale, (y + height) * map_overlay_scale, coord_x2, coord_y2)
    ac.ext_glVertexTex((x + width) * map_overlay_scale, y * map_overlay_scale, coord_x2, coord_y1)
    ac.glEnd()


def player_rivals_overlays_vertex_tex(x, y, width, height, coord_x1=0, coord_x2=1, coord_y1=0, coord_y2=1):
    ac.glBegin(acsys.GL.Quads)
    ac.ext_glVertexTex(x * player_rivals_overlays_scale, y * player_rivals_overlays_scale, coord_x1, coord_y1)
    ac.ext_glVertexTex(x * player_rivals_overlays_scale, (y + height) * player_rivals_overlays_scale, coord_x1, coord_y2)
    ac.ext_glVertexTex((x + width) * player_rivals_overlays_scale, (y + height) * player_rivals_overlays_scale, coord_x2, coord_y2)
    ac.ext_glVertexTex((x + width) * player_rivals_overlays_scale, y * player_rivals_overlays_scale, coord_x2, coord_y1)
    ac.glEnd()


def rear_view_mirror_overlay_vertex_tex(x, y, width, height, coord_x1 = 0, coord_x2 = 1, coord_y1 = 0, coord_y2 = 1):
    ac.glBegin(acsys.GL.Quads)
    ac.ext_glVertexTex(x * rear_view_mirror_overlay_scale, y * rear_view_mirror_overlay_scale, coord_x1, coord_y1)
    ac.ext_glVertexTex(x * rear_view_mirror_overlay_scale, (y + height) * rear_view_mirror_overlay_scale, coord_x1, coord_y2)
    ac.ext_glVertexTex((x + width) * rear_view_mirror_overlay_scale, (y + height) * rear_view_mirror_overlay_scale, coord_x2, coord_y2)
    ac.ext_glVertexTex((x + width) * rear_view_mirror_overlay_scale, y * rear_view_mirror_overlay_scale, coord_x2, coord_y1)
    ac.glEnd()


def time_vertex_tex(x, y, width, height, coord_x1=0, coord_x2=1, coord_y1=0, coord_y2=1):
    ac.glBegin(acsys.GL.Quads)
    ac.ext_glVertexTex(x * time_scale, y * time_scale, coord_x1, coord_y1)
    ac.ext_glVertexTex(x * time_scale, (y + height) * time_scale, coord_x1, coord_y2)
    ac.ext_glVertexTex((x + width) * time_scale, (y + height) * time_scale, coord_x2, coord_y2)
    ac.ext_glVertexTex((x + width) * time_scale, y * time_scale, coord_x2, coord_y1)
    ac.glEnd()
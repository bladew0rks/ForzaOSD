import ac,acsys
from utils import *
from conf import *
from charpool import *
from gears import *
from gears_flash import *
from speedo import *
from revmeter import *
from tyres import *
from laptime import *
from fuel import *
from delta import *
from lap import *
from revgraph import *
from flashzone import *
from valid_lap import *
from flags import *
from speedograph import *  
import g
from maxrpm import *

if g.debug:
  from debug import *

class Dash:

  def __init__(self,appPATH,window):      
    
    skinNAME = ''
    skinNAME = loadCFG(appPATH+'cfg/','master_config.ini','GENERAL','skin'+str(window),skinNAME)
    
    self.thisDashWindow = ac.newApp("PDash-"+skinNAME)
    ac.setTitle(self.thisDashWindow,"")
    noicon(self.thisDashWindow)
                     
    self.skinPATH = appPATH + "skins/" + skinNAME + "/"
    
    background_image = 'none'
    background_image = loadCFG(self.skinPATH,'config.ini','GENERAL','background_image',background_image)        
    
    self.size_x = 100
    self.size_x = int(loadCFG(self.skinPATH,'config.ini','GENERAL','size_x',self.size_x))
    self.size_y = 100
    self.size_y = int(loadCFG(self.skinPATH,'config.ini','GENERAL','size_y',self.size_y))
    
    self.scale = 0
    self.scale = float(loadCFG(self.skinPATH,'config.ini','GENERAL','scale',self.scale))
           
    self.new_lap_armed = False
    self.lap = 0
    self.new_session = False
    self.new_session_armed = False          
    self.was_in_pit = False
    
    self.previous_lap_valid = False
    self.current_lap_valid  = False                
    self.max_tyres_out = 0
    self.max_tyres_out = float(loadCFG(self.skinPATH,'config.ini','GENERAL','max_tyres_out',self.max_tyres_out))          
    
    setsize(self.thisDashWindow, self.size_x * self.scale, self.size_y * self.scale)
    
    self.background = ac.addLabel(self.thisDashWindow, "")
    inscreen(self.background)
    setsize(self.background, self.size_x * self.scale, self.size_y * self.scale)
    texture(self.background, self.skinPATH + "images/" + background_image)        
    
    self.charsets = ''
    self.charsets = loadCFG(self.skinPATH,'config.ini','GENERAL','charsets',self.charsets).split(",")
    
    # prepare chars for the dash
    
    self.char_pool = {}
    
    for charset in self.charsets:
      self.char_pool[charset] = CharPool(self,charset)        
    
    # check if gears is present    
    self.gears_present = readbool(self.skinPATH,'config.ini','GEARS','is_present')
    
    if self.gears_present:
      self.gears = Gears(self)        
          
    # check if speedo present  
        
    self.speedo_present = readbool(self.skinPATH,'config.ini','SPEEDOMETER','is_present')
    
    if self.speedo_present:
      self.speedo = Speedo(self)  

    # check if revmeter is present    
    self.revmeter_present = readbool(self.skinPATH,'config.ini','REVMETER','is_present')
    
    if self.revmeter_present:
      self.revmeter = Revmeter(self)       
      
    # check if tyres_temp is present    
    self.tyres_temp_present = readbool(self.skinPATH,'config.ini','TYRES_TEMP','is_present')
    
    if self.tyres_temp_present:
      self.tyres_temp = Tyres(self,'TEMP')  
      
    # check if tyres_wear is present    
    self.tyres_wear_present = readbool(self.skinPATH,'config.ini','TYRES_WEAR','is_present')
    
    if self.tyres_wear_present:
      self.tyres_wear = Tyres(self,'WEAR')             
    
    # check if laptime is present    
    self.laptime_present = readbool(self.skinPATH,'config.ini','LAPTIME','is_present')
    
    if self.laptime_present:
      self.laptime = LapTime(self,'LAPTIME')           
    
    # check if lastlap is present    
    self.lastlap_present = readbool(self.skinPATH,'config.ini','LASTLAP','is_present')
    
    if self.lastlap_present:
      self.lastlap = LapTime(self,'LASTLAP')    
      
    # check if bestlap is present    
    self.bestlap_present = readbool(self.skinPATH,'config.ini','BESTLAP','is_present')
    
    if self.bestlap_present:
      self.bestlap = LapTime(self,'BESTLAP')             
    
    # check if remaining fuel is present    
    self.fuel_remaining_present = readbool(self.skinPATH,'config.ini','FUELREM','is_present')
    
    if self.fuel_remaining_present:
      self.fuel_rem = Fuel(self,'FUELREM')   
    
    # check if fuel per lap is present    
    self.fuel_per_lap_present = readbool(self.skinPATH,'config.ini','FUELPERLAP','is_present')
    
    if self.fuel_per_lap_present:
      self.fuel_per_lap = Fuel(self,'FUELPERLAP')   
      
    # check if fuel remaining laps is present    
    self.fuel_rem_laps_present = readbool(self.skinPATH,'config.ini','FUELREMLAPS','is_present')
    
    if self.fuel_rem_laps_present:
      self.fuel_rem_laps = Fuel(self,'FUELREMLAPS')     
      
    # check if delta is present    
    self.delta_present = readbool(self.skinPATH,'config.ini','DELTA','is_present')
    
    if self.delta_present:
      self.delta = Delta(self)   
      
    # check if lap number is present    
    self.cur_lap_present = readbool(self.skinPATH,'config.ini','LAP','is_present')
    
    if self.cur_lap_present:
      self.cur_lap = Lap(self)
      
    # check if rev graph is present    
    self.rev_graph_present = readbool(self.skinPATH,'config.ini','REVGRAPH','is_present')
    
    if self.rev_graph_present:
      self.rev_graph = RevGraph(self)              
    
    # check if flash zone is present    
    self.flash_zone_present = readbool(self.skinPATH,'config.ini','FLASHZONE','is_present')
    
    if self.flash_zone_present:
      self.flash_zone = FlashZone(self)   
  
    # check if change gear flash is present    
    self.gears_flash_present = readbool(self.skinPATH,'config.ini','GEARS','flash')
    
    if self.gears_flash_present:
      self.gears_flash = GearsFlash(self)  
      
    # check if valid lap indicator is present    
    self.valid_lap_present = readbool(self.skinPATH,'config.ini','VALIDLAP','is_present')
    
    if self.valid_lap_present:
      self.valid_lap = ValidLap(self)    
      
    # check if flags indicator is present    
    self.flags_present = readbool(self.skinPATH,'config.ini','FLAGS','is_present')
    
    if self.flags_present:
      self.flags = Flags(self)      
  
    # check if speedograph indicator is present    
    self.speedograph_present = readbool(self.skinPATH,'config.ini','SPEEDOGRAPH','is_present')
    
    if self.speedograph_present:
      self.speedograph = SpeedoGraph(self) 
  
    # check if debug is on
    if g.debug:
      self.debug = Debug(self)
      
    # initialize max rpm 
    self.maxrpm = Maxrpm(self)    
  
  def clear_dash(self):    
    for charset in self.charsets:
      self.char_pool[charset].reset_pool()
  
  def string_to_dash(self,s,x,y,c_scale,kerning,charset,dotkern=0.5):      
    self.char_pool[charset].string_to_dash(self,s,x,y,c_scale,kerning,charset,dotkern)    
  
  def is_a_new_lap(self):           
      
    lap_time = ac.getCarState(0, acsys.CS.LapTime)
    now_in_pit = g.sim_info.graphics.isInPit        

    if self.new_lap_armed:      
      self.new_lap = lap_time < 100
      if self.new_lap:
        self.new_lap_armed = False        
        self.lap += 1
    else:
      self.new_lap = False
      self.new_lap_armed = lap_time >= 100
      
    if not self.new_session_armed:
      self.new_session = False
      self.new_session_armed = lap_time > 110                   
      
    if self.new_session_armed:
      self.new_session = lap_time < 150 and ac.getCarState(0, acsys.CS.SpeedKMH) < 5
      self.new_session_armed = False
      
    if self.was_in_pit and now_in_pit == 0:
      self.was_in_pit = False          
    elif not self.was_in_pit and now_in_pit == 1:
      self.was_in_pit = True
      self.new_session = True            
    
    if self.new_session:
      self.lap = 0
      self.new_lap_armed = False
      self.new_session_armed = False            
      self.was_in_pit = False    
  
  def set_validity(self):
       
    if self.new_lap:                                        
      self.previous_lap_valid = self.current_lap_valid          
      self.current_lap_valid = True  
    
    if self.current_lap_valid and g.sim_info.physics.numberOfTyresOut > self.max_tyres_out:    
      self.current_lap_valid = False       
  
  def refresh(self,dt):        
    
    transparent(self.thisDashWindow)
    noborder(self.thisDashWindow)        
    
    self.clear_dash()
    
    if g.debug:
      self.debug.refresh(self)
        
    self.maxrpm.refresh(self)
    
    if self.bestlap_present or self.fuel_per_lap_present or self.valid_lap_present:
      self.is_a_new_lap()
      self.set_validity()      
    
    if self.gears_present:
      self.gears.refresh(self)
      
    if self.gears_flash_present:
      self.gears_flash.refresh(self,dt)
      
    if self.speedo_present:
      self.speedo.refresh(self,dt)
      
    if self.revmeter_present:
      self.revmeter.refresh(self,dt)  
      
    if self.tyres_temp_present:
      self.tyres_temp.refresh(self,dt,'TEMP')    
      
    if self.tyres_wear_present:
      self.tyres_wear.refresh(self,dt,'WEAR')  
      
    if self.laptime_present:
      self.laptime.refresh(self,dt,'LAPTIME')  
      
    if self.lastlap_present:
      self.lastlap.refresh(self,dt,'LASTLAP')  
      
    if self.bestlap_present:
      self.bestlap.refresh(self,dt,'BESTLAP')   
      
    if self.fuel_remaining_present:
      self.fuel_rem.refresh(self,dt,'FUELREM')  
      
    if self.fuel_per_lap_present:
      self.fuel_per_lap.refresh(self,dt,'FUELPERLAP')     
      
    if self.fuel_rem_laps_present:
      self.fuel_rem_laps.refresh(self,dt,'FUELREMLAPS')
      
    if self.delta_present:
      self.delta.refresh(self,dt) 
      
    if self.cur_lap_present:
      self.cur_lap.refresh(self,dt)                         
    
    if self.rev_graph_present:
      self.rev_graph.refresh(self,dt)
      
    if self.flash_zone_present:
      self.flash_zone.refresh(self,dt)                             
      
    if self.valid_lap_present:
      self.valid_lap.refresh(self,dt)        
          
    if self.flags_present:
      self.flags.refresh(self,dt)              
    
    if self.speedograph_present:
      self.speedograph.refresh(self,dt)     
    
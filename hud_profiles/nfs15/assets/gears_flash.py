import ac,acsys
from conf import *
from utils import *

class GearsFlash:

  def __init__(self,MyDash):
    
    self.gears_flash_time = 0
    self.gears_flash_time = float(loadCFG(MyDash.skinPATH,'config.ini','GEARS','flash_time',self.gears_flash_time))
    
    gears_flash_image = 'none'
    gears_flash_image = loadCFG(MyDash.skinPATH,'config.ini','GEARS','background_flash',gears_flash_image)
        
    self.gears_flash = ac.addLabel(MyDash.thisDashWindow, "")
    offscreen(self.gears_flash)
    transparent(self.gears_flash)
    setsize(self.gears_flash, MyDash.size_x * MyDash.scale, MyDash.size_y * MyDash.scale)
    texture(self.gears_flash, MyDash.skinPATH + "images/" + gears_flash_image)
    
    self.previous_gear = 0
    self.gears_flash_is_on = False
    
  def refresh(self,MyDash,dt):
  
    current_gear = ac.getCarState(0, acsys.CS.Gear) - 1
  
    if current_gear != self.previous_gear:
      inscreen(self.gears_flash)      
      self.gears_flash_is_on = True
      self.previous_gear = current_gear
      self.time_since_gear_change = 0
    elif self.gears_flash_is_on:
      self.time_since_gear_change += dt
      if self.time_since_gear_change >= self.gears_flash_time:
        offscreen(self.gears_flash)        
        self.gears_flash_is_on = False
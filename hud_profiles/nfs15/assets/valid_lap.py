import ac,acsys
from conf import *
from utils import *

class ValidLap:

  def __init__(self,MyDash):
    
    image_valid = 'none'
    image_valid = loadCFG(MyDash.skinPATH,'config.ini','VALIDLAP','image_valid',image_valid)
        
    image_invalid = 'none'
    image_invalid = loadCFG(MyDash.skinPATH,'config.ini','VALIDLAP','image_invalid',image_invalid)    
        
    self.validlap = ac.addLabel(MyDash.thisDashWindow, "")
    offscreen(self.validlap)
    transparent(self.validlap)
    setsize(self.validlap, MyDash.size_x * MyDash.scale, MyDash.size_y * MyDash.scale)
    texture(self.validlap, MyDash.skinPATH + "images/" + image_valid)
    
    self.invalidlap = ac.addLabel(MyDash.thisDashWindow, "")
    offscreen(self.invalidlap)
    transparent(self.invalidlap)
    setsize(self.invalidlap, MyDash.size_x * MyDash.scale, MyDash.size_y * MyDash.scale)
    texture(self.invalidlap, MyDash.skinPATH + "images/" + image_invalid)           
    
  def refresh(self,MyDash,dt):
  
    if MyDash.current_lap_valid:
      offscreen(self.invalidlap)
      inscreen(self.validlap)
    else:
      offscreen(self.validlap)
      inscreen(self.invalidlap)         
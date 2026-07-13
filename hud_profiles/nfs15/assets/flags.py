import ac,acsys
from conf import *
from utils import *
import g

class Flags:

  def __init__(self,MyDash):
        
    self.flag  = []
    
    for i in range(6):    
      flag_image = 'none'
      flag_image = loadCFG(MyDash.skinPATH,'config.ini','FLAGS','image'+str(i+1),flag_image)        
          
      self.flag.append(ac.addLabel(MyDash.thisDashWindow, ""))
      offscreen(self.flag[i])
      transparent(self.flag[i])
      setsize(self.flag[i], MyDash.size_x * MyDash.scale, MyDash.size_y * MyDash.scale)
      texture(self.flag[i], MyDash.skinPATH + "images/flags/" + flag_image)         
    
    self.previous_flag = 0
    
  def refresh(self,MyDash,dt):
  
    current_flag = g.sim_info.graphics.flags
  
    if current_flag != self.previous_flag:
      if self.previous_flag != 0:
        offscreen(self.flag[self.previous_flag-1])
      if current_flag != 0:
        inscreen(self.flag[current_flag-1])  
      self.previous_flag = current_flag      
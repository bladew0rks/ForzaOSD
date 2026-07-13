from conf import *
import acsys

class Gears:

  def __init__(self,MyDash):
        
    self.pos_x = 0
    self.pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','GEARS','pos_x',self.pos_x))
    self.pos_y = 0
    self.pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','GEARS','pos_y',self.pos_y))
    self.scale = 1
    self.scale = float(loadCFG(MyDash.skinPATH,'config.ini','GEARS','scale',self.scale))
    self.charset = ' '
    self.charset = loadCFG(MyDash.skinPATH,'config.ini','GEARS','charset',self.charset) 
      
  def refresh(self,MyDash):
    
    current_gear = ac.getCarState(0, acsys.CS.Gear) - 1          
    if current_gear == -1: current_gear = 'R'
    if current_gear ==  0: current_gear = 'N'
    MyDash.string_to_dash(str(current_gear), self.pos_x, self.pos_y, self.scale, 1, self.charset)
    
import ac,acsys
from utils import *
from conf import *
import g

class Maxrpm:

  def __init__(self,MyDash):
  
    self.status = 'calib'
  
  def refresh(self,MyDash):
     
    if self.status == 'ok':
      return True    
     
    if self.status == 'calib' and g.sim_info.static.maxRpm > 0:
      g.maxrpm = g.sim_info.static.maxRpm
      self.status = 'ok'
      return True
    
    if self.status == 'calib':                    
      g.maxrpm_calculated = True
      g.maxrpm = max(ac.getCarState(0, acsys.CS.RPM),g.maxrpm)    
      if ac.getCarState(0, acsys.CS.SpeedKMH) > 150: self.status = 'ok'      
      return True
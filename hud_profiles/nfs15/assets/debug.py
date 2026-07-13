import ac,acsys
from utils import *
import g

class Debug:

  def __init__(self,MyDash):
  
    a = 1
  
  def refresh(self,MyDash):
  
    tocons('maxRpm:           ' + str(g.sim_info.static.maxRpm))
    tocons('isInPit:          ' + str(g.sim_info.graphics.isInPit))
    tocons('numberOfTyresOut: ' + str(g.sim_info.physics.numberOfTyresOut))
    tocons('flags:            ' + str(g.sim_info.graphics.flags))
    tocons('fuel:             ' + str(g.sim_info.physics.fuel))
    tocons('distanceTraveled: ' + str(g.sim_info.graphics.distanceTraveled))
    tocons('iLastTime:        ' + str(g.sim_info.graphics.iLastTime))
    tocons('tyreWear:         ' + str(g.sim_info.physics.tyreWear[0]) + '/' + str(g.sim_info.physics.tyreWear[1]) + '/' + str(g.sim_info.physics.tyreWear[2]) + '/' + str(g.sim_info.physics.tyreWear[3]))
 
    tocons('LapTime:          ' + str(ac.getCarState(0, acsys.CS.LapTime)))
    tocons('SpeedKMH:         ' + str(ac.getCarState(0, acsys.CS.SpeedKMH)))
    tocons('SpeedMPH:         ' + str(ac.getCarState(0, acsys.CS.SpeedMPH)))
    tocons('delta:            ' + str(ac.getCarState(0, acsys.CS.PerformanceMeter)))
    tocons('rpm:              ' + str(ac.getCarState(0, acsys.CS.RPM)))
    tocons('gear:             ' + str(ac.getCarState(0, acsys.CS.Gear)))
    tocons('laptime:          ' + str(ac.getCarState(0, acsys.CS.LapTime)))
    tocons('tyreTemp:         ' + str(ac.getCarState(0, acsys.CS.CurrentTyresCoreTemp)[0]) + '/' + str(ac.getCarState(0, acsys.CS.CurrentTyresCoreTemp)[1]) + '/' + str(ac.getCarState(0, acsys.CS.CurrentTyresCoreTemp)[2]) + '/' + str(ac.getCarState(0, acsys.CS.CurrentTyresCoreTemp)[3]))
 
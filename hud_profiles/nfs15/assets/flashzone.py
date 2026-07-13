import ac,acsys
from conf import *
from utils import *
import g

class FlashZone:

  def __init__(self,MyDash):

    self.limits = []
    self.limits = loadCFG(MyDash.skinPATH,'config.ini','FLASHZONE','limits',self.limits).split(',')
    self.limits = [float(x) for x in self.limits]

    self.nzones = len(self.limits)
    self.limits.append(150.0)

    self.last_zone_flash = readbool(MyDash.skinPATH,'config.ini','FLASHZONE','last_zone_flash')

    if self.last_zone_flash:
      self.last_zone_flash_freq = 0
      self.last_zone_flash_freq = float(loadCFG(MyDash.skinPATH,'config.ini','FLASHZONE','last_zone_flash_freq',self.last_zone_flash_freq))
      self.flash_off = self.flash_on = 0

    self.zones = []

    for i in range(self.nzones):
      self.zones.append(ac.addLabel(MyDash.thisDashWindow, ""))
      offscreen(self.zones[-1])
      transparent(self.zones[-1])
      setsize(self.zones[-1], MyDash.size_x * MyDash.scale, MyDash.size_y * MyDash.scale)
      texture(self.zones[-1], MyDash.skinPATH + "images/revzones/" + str(i+1) + '.png')

    self.first_time = True    

  def refresh(self,MyDash,dt):

    rpm = ac.getCarState(0,acsys.CS.RPM)    

    if self.first_time or g.maxrpm_calculated:
      self.first_time = False
      self.limits_rpm = [(x/100*g.maxrpm) for x in self.limits]
    
    for i in range(self.nzones):
      offscreen(self.zones[i])

    for i in range(self.nzones):
      if i < (self.nzones-1) or not self.last_zone_flash:
        if rpm >= self.limits_rpm[i] and rpm < self.limits_rpm[i+1]:
          inscreen(self.zones[i])
          self.flash_off = self.flash_on = 0
      else:
        if rpm >= self.limits_rpm[i]:
          if self.flash_on <= (1/self.last_zone_flash_freq):
            self.flash_on += dt
            inscreen(self.zones[i])
          else:
            self.flash_off += dt
            if self.flash_off >= (1/self.last_zone_flash_freq):
              self.flash_off = self.flash_on = 0

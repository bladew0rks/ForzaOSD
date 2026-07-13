from conf import *
import ac,acsys
from utils import *
import math
import g

class SpeedoGraph:

  def __init__(self,MyDash):
    
    self.limits_uom = ''
    self.limits_uom = loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','limits_uom',self.limits_uom)
    
    self.limits = []
    self.limits = loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','limits',self.limits).split(',')
    self.limits = [float(x) for x in self.limits]     
    
    self.size_x = 0
    self.size_x = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','size_x',self.size_x))
    self.size_y = 0
    self.size_y = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','size_y',self.size_y))            
    
    self.prefix = ' '
    self.prefix = loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','prefix',self.prefix)   
     
    self.pos_x = 0
    self.pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','pos_x',self.pos_x))
    self.pos_y = 0
    self.pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','pos_y',self.pos_y))
    self.scale_x = 0
    self.scale_x = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','scale_x',self.scale_x))
    self.scale_y = 0
    self.scale_y = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','scale_y',self.scale_y))
        
    self.always_show_frame_zero = readbool(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','always_show_frame_zero')
        
    self.label_present = readbool(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','label_present')
    
    if self.label_present:
      self.label = 0
      self.label = loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','label',self.label)
      self.label_pos_x = 0
      self.label_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','label_pos_x',self.label_pos_x))
      self.label_pos_y = 0
      self.label_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','label_pos_y',self.label_pos_y))
      self.label_scale = 0
      self.label_scale = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','label_scale',self.label_scale))    
      self.label_kerning = 0
      self.label_kerning = float(loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','label_kerning',self.label_kerning))
      self.label_charset = ' '
      self.label_charset = loadCFG(MyDash.skinPATH,'config.ini','SPEEDOGRAPH','label_charset',self.label_charset)    
            
    self.load_frames(MyDash)
    self.last_frame_used = -1      

  def load_frames(self,MyDash):
    
    self.frames = []
    
    for i in range(len(self.limits)):
      self.frames.append(ac.addLabel(MyDash.thisDashWindow, ""))
      offscreen(self.frames[-1])
      transparent(self.frames[-1])      
      texture(self.frames[-1], MyDash.skinPATH + "images/speed/" + self.prefix + str(i) + ".png")
      
    if self.always_show_frame_zero:
      setpos(self.frames[0], self.pos_x * MyDash.size_x * MyDash.scale , self.pos_y * MyDash.size_y * MyDash.scale)
      setsize(self.frames[0], self.size_x * self.scale_x * MyDash.scale, self.size_y * self.scale_y * MyDash.scale)      
  
  def near(self,lim,speed):
  
    if speed < lim[0]:  return 0
    if speed > lim[-1]: return len(lim)-1
    
    for v in lim[:-1]:
     v_next = lim[lim.index(v)+1]
     if speed >= v and speed < v_next:
       v_mean = (v+v_next)/2
       if speed < v_mean:
         return lim.index(v)
       else:
         return lim.index(v_next)
  
  def refresh(self,MyDash,dt):        
              
    speed = ac.getCarState(0,acsys.CS.SpeedKMH) if self.limits_uom == 'KPH' else ac.getCarState(0,acsys.CS.SpeedMPH)                    
    
    if self.last_frame_used >= 0:
      if (self.last_frame_used == 0 and not self.always_show_frame_zero) or self.last_frame_used > 0:
        offscreen(self.frames[self.last_frame_used])
    
    if (speed >= self.limits[0]): 
      self.last_frame_used = self.near(self.limits,speed)
      setpos(self.frames[self.last_frame_used], self.pos_x * MyDash.size_x * MyDash.scale , self.pos_y * MyDash.size_y * MyDash.scale)
      setsize(self.frames[self.last_frame_used], self.size_x * self.scale_x * MyDash.scale, self.size_y * self.scale_y * MyDash.scale)      
    else:
      self.last_frame_used = -1  
    
    if self.label_present:
      MyDash.string_to_dash(self.label, self.label_pos_x, self.label_pos_y, self.label_scale, self.label_kerning, self.label_charset)
    
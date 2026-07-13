from conf import *
import ac,acsys
from utils import *
import math
import g

class RevGraph:

  def __init__(self,MyDash):
    
    self.low_rev = 0
    self.low_rev = int(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','low_rev_perc',self.low_rev))/100
    
    self.high_rev = 0
    self.high_rev = int(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','high_rev_perc',self.high_rev))/100        
    
    self.size_x = 0
    self.size_x = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','size_x',self.size_x))
    self.size_y = 0
    self.size_y = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','size_y',self.size_y))
    
    self.frames_n = 0
    self.frames_n = int(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','frames',self.frames_n))
    
    self.prefix = ' '
    self.prefix = loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','prefix',self.prefix)   
     
    self.pos_x = 0
    self.pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','pos_x',self.pos_x))
    self.pos_y = 0
    self.pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','pos_y',self.pos_y))
    self.scale_x = 0
    self.scale_x = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','scale_x',self.scale_x))
    self.scale_y = 0
    self.scale_y = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','scale_y',self.scale_y))
        
    self.always_show_frame_zero = readbool(MyDash.skinPATH,'config.ini','REVGRAPH','always_show_frame_zero')
        
    self.label_present = readbool(MyDash.skinPATH,'config.ini','REVGRAPH','label_present')
    
    if self.label_present:
      self.label = 0
      self.label = loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','label',self.label)
      self.label_pos_x = 0
      self.label_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','label_pos_x',self.label_pos_x))
      self.label_pos_y = 0
      self.label_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','label_pos_y',self.label_pos_y))
      self.label_scale = 0
      self.label_scale = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','label_scale',self.label_scale))    
      self.label_kerning = 0
      self.label_kerning = float(loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','label_kerning',self.label_kerning))
      self.label_charset = ' '
      self.label_charset = loadCFG(MyDash.skinPATH,'config.ini','REVGRAPH','label_charset',self.label_charset)    
            
    self.load_frames(MyDash)
    self.last_frame_used = -1      

  def load_frames(self,MyDash):
    
    self.frames = []
    
    for i in range(self.frames_n):
      self.frames.append(ac.addLabel(MyDash.thisDashWindow, ""))
      offscreen(self.frames[-1])
      transparent(self.frames[-1])      
      texture(self.frames[-1], MyDash.skinPATH + "images/rev/" + self.prefix + str(i) + ".png")
      
    if self.always_show_frame_zero:
      setpos(self.frames[0], self.pos_x * MyDash.size_x * MyDash.scale , self.pos_y * MyDash.size_y * MyDash.scale)
      setsize(self.frames[0], self.size_x * self.scale_x * MyDash.scale, self.size_y * self.scale_y * MyDash.scale)      
            
  def refresh(self,MyDash,dt):
    
    low_rpm   = self.low_rev * g.maxrpm
    high_rpm  = self.high_rev * g.maxrpm
    rpm_range = high_rpm - low_rpm
              
    rpm = ac.getCarState(0,acsys.CS.RPM)    
    
    if self.last_frame_used >= 0: offscreen(self.frames[self.last_frame_used])
    
    if (rpm >= low_rpm and rpm < high_rpm+50): 
      self.last_frame_used = min(math.floor((rpm-low_rpm)/rpm_range*(self.frames_n-1))+1,self.frames_n-1)
      setpos(self.frames[self.last_frame_used], self.pos_x * MyDash.size_x * MyDash.scale , self.pos_y * MyDash.size_y * MyDash.scale)
      setsize(self.frames[self.last_frame_used], self.size_x * self.scale_x * MyDash.scale, self.size_y * self.scale_y * MyDash.scale)      
    else:
      self.last_frame_used = -1  
    
    if self.label_present:
      MyDash.string_to_dash(self.label, self.label_pos_x, self.label_pos_y, self.label_scale, self.label_kerning, self.label_charset)
    
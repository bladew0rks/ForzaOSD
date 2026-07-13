from conf import *
from utils import *
import ac,acsys
import math
import g
from utils import *

class LapTime:

  def __init__(self,MyDash,info):
     
    self.pos_x = 0
    self.pos_x = float(loadCFG(MyDash.skinPATH,'config.ini',info,'pos_x',self.pos_x))
    self.pos_y = 0
    self.pos_y = float(loadCFG(MyDash.skinPATH,'config.ini',info,'pos_y',self.pos_y))
    self.scale = 0
    self.scale = float(loadCFG(MyDash.skinPATH,'config.ini',info,'scale',self.scale))
    self.kerning = 0
    self.kerning = float(loadCFG(MyDash.skinPATH,'config.ini',info,'kerning',self.kerning))
    self.charset = ' '
    self.charset = loadCFG(MyDash.skinPATH,'config.ini',info,'charset',self.charset)        
    
    self.format_string_mm = ' '
    self.format_string_mm = loadCFG(MyDash.skinPATH,'config.ini',info,'format_string_mm',self.format_string_mm)    
    
    self.format_string_ss = ' '
    self.format_string_ss = loadCFG(MyDash.skinPATH,'config.ini',info,'format_string_ss',self.format_string_ss)   
    
    self.format_string_ms = ' '
    self.format_string_ms = loadCFG(MyDash.skinPATH,'config.ini',info,'format_string_ms',self.format_string_ms)    
    
    self.digits_ms = ' '
    self.digits_ms = int(loadCFG(MyDash.skinPATH,'config.ini',info,'digits_ms',self.digits_ms))
    
    self.dotkern = 0
    self.dotkern = float(loadCFG(MyDash.skinPATH,'config.ini',info,'dotkern',self.dotkern))
        
    self.label_present = readbool(MyDash.skinPATH,'config.ini',info,'label_present')
    
    if self.label_present:
      self.label = 0
      self.label = loadCFG(MyDash.skinPATH,'config.ini',info,'label',self.label)
      self.label_pos_x = 0
      self.label_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini',info,'label_pos_x',self.label_pos_x))
      self.label_pos_y = 0
      self.label_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini',info,'label_pos_y',self.label_pos_y))
      self.label_scale = 0
      self.label_scale = float(loadCFG(MyDash.skinPATH,'config.ini',info,'label_scale',self.label_scale)) 
      self.label_kerning = 0
      self.label_kerning = float(loadCFG(MyDash.skinPATH,'config.ini',info,'label_kerning',self.label_kerning))
      self.label_charset = ' '
      self.label_charset = loadCFG(MyDash.skinPATH,'config.ini',info,'label_charset',self.label_charset)                 
    
    if info == 'BESTLAP':
      self.best = 99*60*1000            
      self.only_if_valid = readbool(MyDash.skinPATH,'config.ini',info,'only_if_valid')      
            
  def ms_to_str(self,t):    
    if t != 0:
      mm = int(math.floor((t/1000)/60))
      t -= mm*60*1000
      ss = int(math.floor(t/1000))      
      ms = math.floor((t - ss*1000)/(10**(3-self.digits_ms)))
      return self.format_string_mm.format(mm) + ':' + self.format_string_ss.format(ss) + '.' + self.format_string_ms.format(ms)
    else:
      return ' -:--.' + '-'*self.digits_ms  
                
  def refresh(self,MyDash,dt,info):                    
              
    if info == 'LAPTIME': t = ac.getCarState(0,acsys.CS.LapTime)
    if info == 'LASTLAP': t = g.sim_info.graphics.iLastTime
    
    if info == 'BESTLAP':
                  
      last = g.sim_info.graphics.iLastTime
      
      if last > 0:
        if (not self.only_if_valid) or (self.only_if_valid and MyDash.previous_lap_valid):
          self.best = min(self.best,last)
        MyDash.string_to_dash(self.ms_to_str(self.best if self.best != 99*60*1000 else 0), self.pos_x, self.pos_y, self.scale, self.kerning, self.charset, self.dotkern)                   
      else:
        MyDash.string_to_dash(self.ms_to_str(0), self.pos_x, self.pos_y, self.scale, self.kerning, self.charset)                     
    else:  
      MyDash.string_to_dash(self.ms_to_str(t), self.pos_x, self.pos_y, self.scale, self.kerning, self.charset, self.dotkern)                   
    
    if self.label_present:
      MyDash.string_to_dash(self.label, self.label_pos_x, self.label_pos_y, self.label_scale, self.label_kerning, self.label_charset)
          
from conf import *
from utils import *
import ac,acsys

class Delta:

  def __init__(self,MyDash):
     
    self.uom = 0
    self.uom = loadCFG(MyDash.skinPATH,'config.ini','DELTA','uom',self.uom)
    
    self.pos_x = 0
    self.pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','pos_x',self.pos_x))
    self.pos_y = 0
    self.pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','pos_y',self.pos_y))
    self.scale = 0
    self.scale = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','scale',self.scale))   
    self.kerning = 0
    self.kerning = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','kerning',self.kerning)) 
    self.dotkern = 0
    self.dotkern = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','dotkern',self.dotkern))    
    self.dashes = 0
    self.dashes = int(loadCFG(MyDash.skinPATH,'config.ini','DELTA','dashes',self.dashes))    
    self.charset = ' '
    self.charset = loadCFG(MyDash.skinPATH,'config.ini','DELTA','charset',self.charset)    
    
    self.format_string = ' '
    self.format_string = loadCFG(MyDash.skinPATH,'config.ini','DELTA','format_string',self.format_string)    
        
    self.label_present = readbool(MyDash.skinPATH,'config.ini','DELTA','label_present')
    
    if self.label_present:
      self.label = 0
      self.label = loadCFG(MyDash.skinPATH,'config.ini','DELTA','label',self.label)
      self.label_pos_x = 0
      self.label_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','label_pos_x',self.label_pos_x))
      self.label_pos_y = 0
      self.label_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','label_pos_y',self.label_pos_y))
      self.label_scale = 0
      self.label_scale = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','label_scale',self.label_scale))    
      self.label_kerning = 0
      self.label_kerning = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','label_kerning',self.label_kerning))
      self.label_charset = ' '
      self.label_charset = loadCFG(MyDash.skinPATH,'config.ini','DELTA','label_charset',self.label_charset)       
          
    self.uom_label_present = readbool(MyDash.skinPATH,'config.ini','DELTA','uom_label_present')
    
    if self.uom_label_present:      
      self.uom_pos_x = 0
      self.uom_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','uom_pos_x',self.uom_pos_x))
      self.uom_pos_y = 0
      self.uom_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','uom_pos_y',self.uom_pos_y))
      self.uom_scale = 0
      self.uom_scale = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','uom_scale',self.uom_scale))
      self.uom_kerning = 0
      self.uom_kerning = float(loadCFG(MyDash.skinPATH,'config.ini','DELTA','uom_kerning',self.uom_kerning))           
      self.uom_charset = ' '
      self.uom_charset = loadCFG(MyDash.skinPATH,'config.ini','DELTA','uom_charset',self.uom_charset)    
      
  def refresh(self,MyDash,dt):
    
    delta = ac.getCarState(0, acsys.CS.PerformanceMeter)
    
    if MyDash.lap >= 2:              
      delta = self.format_string.format(delta)
    else:
      delta = '-' * self.dashes
      
    MyDash.string_to_dash(delta, self.pos_x, self.pos_y, self.scale, self.kerning, self.charset,self.dotkern)
    
    if self.label_present:
      MyDash.string_to_dash(self.label, self.label_pos_x, self.label_pos_y, self.label_scale, self.label_kerning, self.label_charset)
      
    if self.uom_label_present:
      MyDash.string_to_dash(self.uom, self.uom_pos_x, self.uom_pos_y, self.uom_scale, self.uom_kerning, self.uom_charset)
    
from conf import *
import ac,acsys
import g
from utils import *

class Tyres:

  def __init__(self,MyDash,info):
     
    self.uom = 0
    self.uom = loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'uom',self.uom)
    self.uom = '%' if self.uom == 'percent' else self.uom
    
    self.scale = 0
    self.scale = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'scale',self.scale)) 
    self.format_string = ' '
    self.format_string = loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'format_string',self.format_string)    
     
    self.kerning = 0
    self.kerning = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'kerning',self.kerning))
    self.charset = ' '
    self.charset = loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'charset',self.charset)       
    
    self.pos_fl_x = 0
    self.pos_fl_x = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'pos_fl_x',self.pos_fl_x))
    self.pos_fl_y = 0
    self.pos_fl_y = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'pos_fl_y',self.pos_fl_y))
    
    self.pos_fr_x = 0
    self.pos_fr_x = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'pos_fr_x',self.pos_fr_x))
    self.pos_fr_y = 0
    self.pos_fr_y = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'pos_fr_y',self.pos_fr_y))
    
    self.pos_rl_x = 0
    self.pos_rl_x = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'pos_rl_x',self.pos_rl_x))
    self.pos_rl_y = 0
    self.pos_rl_y = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'pos_rl_y',self.pos_rl_y))
    
    self.pos_rr_x = 0
    self.pos_rr_x = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'pos_rr_x',self.pos_rr_x))
    self.pos_rr_y = 0
    self.pos_rr_y = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'pos_rr_y',self.pos_rr_y))
            
    self.label_present = readbool(MyDash.skinPATH,'config.ini','TYRES_' + info,'label_present')
    
    if self.label_present:
      self.label = 0
      self.label = loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'label',self.label)
      self.label_pos_x = 0
      self.label_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'label_pos_x',self.label_pos_x))
      self.label_pos_y = 0
      self.label_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'label_pos_y',self.label_pos_y))
      self.label_scale = 0
      self.label_scale = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'label_scale',self.label_scale))
      self.label_kerning = 0
      self.label_kerning = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'label_kerning',self.label_kerning))
      self.label_charset = ' '
      self.label_charset = loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'label_charset',self.label_charset)              
          
    self.uom_label_present = readbool(MyDash.skinPATH,'config.ini','TYRES_' + info,'uom_label_present')
    
    if self.uom_label_present:      
      self.uom_pos_x = 0
      self.uom_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'uom_pos_x',self.uom_pos_x))
      self.uom_pos_y = 0
      self.uom_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'uom_pos_y',self.uom_pos_y))
      self.uom_scale = 0
      self.uom_scale = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'uom_scale',self.uom_scale))
      self.uom_kerning = 0
      self.uom_kerning = float(loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'uom_kerning',self.uom_kerning))
      self.uom_charset = ' '
      self.uom_charset = loadCFG(MyDash.skinPATH,'config.ini','TYRES_' + info,'uom_charset',self.uom_charset)                     
  
  def to_fahrenheit(self,c):
    return (c*9/5)+32
      
  def refresh(self,MyDash,dt,info):
              
    if info == 'TEMP':
      fl,fr,rl,rr = ac.getCarState(0, acsys.CS.CurrentTyresCoreTemp)
    else:
      fl,fr,rl,rr = g.sim_info.physics.tyreWear
    
    if info == 'TEMP' and self.uom == 'F':
      fl = self.to_fahrenheit(fl)
      fr = self.to_fahrenheit(fr)
      rl = self.to_fahrenheit(rl)
      rr = self.to_fahrenheit(rr)
        
    MyDash.string_to_dash(self.format_string.format(fl), self.pos_fl_x, self.pos_fl_y, self.scale, self.kerning, self.charset)
    MyDash.string_to_dash(self.format_string.format(fr), self.pos_fr_x, self.pos_fr_y, self.scale, self.kerning, self.charset)
    MyDash.string_to_dash(self.format_string.format(rl), self.pos_rl_x, self.pos_rl_y, self.scale, self.kerning, self.charset)
    MyDash.string_to_dash(self.format_string.format(rr), self.pos_rr_x, self.pos_rr_y, self.scale, self.kerning, self.charset)
    
    if self.label_present:
      MyDash.string_to_dash(self.label, self.label_pos_x, self.label_pos_y, self.label_scale, self.label_kerning, self.label_charset)
      
    if self.uom_label_present:
      MyDash.string_to_dash(self.uom, self.uom_pos_x, self.uom_pos_y, self.uom_scale, self.uom_kerning, self.uom_charset)        
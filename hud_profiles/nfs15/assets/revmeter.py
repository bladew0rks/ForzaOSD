from conf import *
from utils import *
import ac,acsys

class Revmeter:

  def __init__(self,MyDash):
     
    self.pos_x = 0
    self.pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','REVMETER','pos_x',self.pos_x))
    self.pos_y = 0
    self.pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','REVMETER','pos_y',self.pos_y))
    self.scale = 0
    self.scale = float(loadCFG(MyDash.skinPATH,'config.ini','REVMETER','scale',self.scale))
    self.kerning = 0
    self.kerning = float(loadCFG(MyDash.skinPATH,'config.ini','REVMETER','kerning',self.kerning))
    self.charset = ' '
    self.charset = loadCFG(MyDash.skinPATH,'config.ini','REVMETER','charset',self.charset)              
    
    self.format_string = ' '
    self.format_string = loadCFG(MyDash.skinPATH,'config.ini','REVMETER','format_string',self.format_string)    
        
    self.label_present = readbool(MyDash.skinPATH,'config.ini','REVMETER','label_present')
    
    if self.label_present:
      self.label = 0
      self.label = loadCFG(MyDash.skinPATH,'config.ini','REVMETER','label',self.label)
      self.label_pos_x = 0
      self.label_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini','REVMETER','label_pos_x',self.label_pos_x))
      self.label_pos_y = 0
      self.label_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini','REVMETER','label_pos_y',self.label_pos_y))
      self.label_scale = 0
      self.label_scale = float(loadCFG(MyDash.skinPATH,'config.ini','REVMETER','label_scale',self.label_scale))    
      self.label_kerning = 0
      self.label_kerning = float(loadCFG(MyDash.skinPATH,'config.ini','REVMETER','label_kerning',self.label_kerning))   
      self.label_charset = ' '
      self.label_charset = loadCFG(MyDash.skinPATH,'config.ini','REVMETER','label_charset',self.label_charset)              
            
  def refresh(self,MyDash,dt):
              
    rpm = ac.getCarState(0,acsys.CS.RPM)    
    MyDash.string_to_dash(self.format_string.format(rpm), self.pos_x, self.pos_y, self.scale, self.kerning, self.charset)
    
    if self.label_present:
      MyDash.string_to_dash(self.label, self.label_pos_x, self.label_pos_y, self.label_scale, self.label_kerning, self.label_charset)
    
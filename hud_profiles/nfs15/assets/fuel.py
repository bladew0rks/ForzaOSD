from conf import *
from utils import *
import ac,acsys
import g

class Fuel:

  def __init__(self,MyDash,info):
    
    self.uom = 0
    self.uom = loadCFG(MyDash.skinPATH,'config.ini',info,'uom',self.uom)
     
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
    
    self.format_string = ' '
    self.format_string = loadCFG(MyDash.skinPATH,'config.ini',info,'format_string',self.format_string)    
        
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
          
    self.uom_label_present = readbool(MyDash.skinPATH,'config.ini',info,'uom_label_present')
    
    if self.uom_label_present:      
      self.uom_pos_x = 0
      self.uom_pos_x = float(loadCFG(MyDash.skinPATH,'config.ini',info,'uom_pos_x',self.uom_pos_x))
      self.uom_pos_y = 0
      self.uom_pos_y = float(loadCFG(MyDash.skinPATH,'config.ini',info,'uom_pos_y',self.uom_pos_y))
      self.uom_scale = 0
      self.uom_scale = float(loadCFG(MyDash.skinPATH,'config.ini',info,'uom_scale',self.uom_scale))
      self.uom_kerning = 0
      self.uom_kerning = float(loadCFG(MyDash.skinPATH,'config.ini',info,'uom_kerning',self.uom_kerning))
      self.uom_charset = ' '
      self.uom_charset = loadCFG(MyDash.skinPATH,'config.ini',info,'uom_charset',self.uom_charset)                                    
            
  def refresh(self,MyDash,dt,info):
              
    if info == 'FUELREM':
      out = g.sim_info.physics.fuel     
    
    if info == 'FUELPERLAP' or info == 'FUELREMLAPS':
      
      if MyDash.lap < 2:
        self.fpl = 0
        self.frl = 0
        
      if MyDash.new_lap:
        
        if MyDash.lap == 2:
          self.track_length = g.sim_info.graphics.distanceTraveled - self.start_of_track
        
        if MyDash.lap == 1 or MyDash.new_session:
          self.initial_fuel = g.sim_info.physics.fuel
          self.start_of_track = g.sim_info.graphics.distanceTraveled          
        else:
          self.initial_fuel_minus_1 = self.initial_fuel
          self.initial_fuel = g.sim_info.physics.fuel
          self.distance_at_start_of_track = g.sim_info.graphics.distanceTraveled                    
      
      if MyDash.lap >= 2:
        actual_fuel = g.sim_info.physics.fuel                
        self.fpl = (self.initial_fuel_minus_1 - actual_fuel) / (1+(g.sim_info.graphics.distanceTraveled-self.distance_at_start_of_track)/self.track_length)        
        if info == 'FUELREMLAPS':
          self.frl = (actual_fuel / self.fpl) if self.fpl != 0 else 0
          
      if info == 'FUELPERLAP':  out = self.fpl    
      if info == 'FUELREMLAPS': out = self.frl
                
    if self.uom == 'GL': out = out * 0.264172052
        
    if MyDash.lap >= 2 or info == 'FUELREM':  
      out = self.format_string.format(out)
    else:
      out = '----'
        
    MyDash.string_to_dash(out, self.pos_x, self.pos_y, self.scale, self.kerning, self.charset)  
   
    if self.label_present:
      MyDash.string_to_dash(self.label, self.label_pos_x, self.label_pos_y, self.label_scale, self.label_kerning, self.label_charset)
      
    if self.uom_label_present:
      MyDash.string_to_dash(self.uom, self.uom_pos_x, self.uom_pos_y, self.uom_scale, self.uom_kerning, self.uom_charset)        
    
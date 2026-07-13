from conf import *
from utils import *
import ac

class CharPool:

  def __init__(self,MyDash,name):
    
    self.chars_size_x = 0
    self.chars_size_x = int(loadCFG(MyDash.skinPATH,'config.ini','CHARS-'+name.upper(),'chars_size_x',self.chars_size_x))
    
    self.chars_size_y = 0
    self.chars_size_y = int(loadCFG(MyDash.skinPATH,'config.ini','CHARS-'+name.upper(),'chars_size_y',self.chars_size_y))
    
    self.allc = ''
    self.allc = list(loadCFG(MyDash.skinPATH,'config.ini','CHARS-'+name.upper(),'chars',self.allc))
    
    self.chars      = {}
    self.used_chars = {}    
    
    for c in self.allc:
      self.chars[c]      = []
      self.used_chars[c] = []
          
    for c in self.allc:            
      self.chars[c].append(ac.addLabel(MyDash.thisDashWindow, ""))
      offscreen(self.chars[c][-1])
      transparent(self.chars[c][-1])
      cc = c
      if cc == '.': cc = 'dot'
      if cc == ':': cc = 'colon'
      if cc == ' ': cc = 'space'
      if cc == '%': cc = 'percent'
      if cc == '/': cc = 'slash'
      if cc == '-': cc = 'minus'
      if cc == '+': cc = 'plus'
      texture(self.chars[c][-1], MyDash.skinPATH + "images/chars/" + name + "/" + cc + ".png")          
      
  def get_char(self,MyDash,c,name):
    
    if self.chars[c] == []:              
      self.chars[c].append(ac.addLabel(MyDash.thisDashWindow, ""))
      transparent(self.chars[c][-1])
      cc = c
      if cc == '.': cc = 'dot'
      if cc == ':': cc = 'colon'
      if cc == ' ': cc = 'space'
      if cc == '%': cc = 'percent'
      if cc == '/': cc = 'slash'
      if cc == '-': cc = 'minus'
      if cc == '+': cc = 'plus'  
      texture(self.chars[c][-1], MyDash.skinPATH + "images/chars/" + name + "/" + cc + ".png")          
      
  def reset_pool(self):

    for c in self.used_chars:
      while self.used_chars[c] != []:
        self.chars[c].append(self.used_chars[c].pop())
        offscreen(self.chars[c][-1])      

  def string_to_dash(self,MyDash,s,x,y,c_scale,kerning,name,dotkern):  

    delta = ((self.chars_size_x / MyDash.size_x) * c_scale) * kerning
    old_c = ''
    
    for c in s:
      if c in self.allc:
        if (c=='.' or c==':' or old_c=='.' or old_c==':') and old_c!='':
          x-= delta*dotkern
        self.get_char(MyDash,c,name)
        self.used_chars[c].append(self.chars[c].pop())
        setpos(self.used_chars[c][-1], x * MyDash.size_x * MyDash.scale , y * MyDash.size_y * MyDash.scale)
        setsize(self.used_chars[c][-1], self.chars_size_x * c_scale * MyDash.scale, self.chars_size_y * c_scale * MyDash.scale)
        x += delta
        old_c = c
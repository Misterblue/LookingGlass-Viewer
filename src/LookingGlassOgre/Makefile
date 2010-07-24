# LookingGlassOgre
Target = LookingGlassOgre
Sources = $(wildcard *.cpp)

CPPFLAGS = -I../../lib/Ogre/include -I../../lib/boost_1_40_0
CXXFLAGS += -Wall -O3
# CXXFLAGS += -g -D_DEBUG

ifeq ($(HOSTTYPE), x86_64)
LIBSELECT=64
endif

all: all_linux

# target specific settings
all_linux: SYSTEM=Linux
all_linux: LDFLAGS = -L/usr/X11R6/lib$(LIBSELECT) -L../lib/$(SYSTEM) -lOgre  -lGL

all_win32 clean_win32: SYSTEM=Win32-gcc
all_win32: LDFLAGS = -L../lib/$(SYSTEM) -lOgre -lopengl32 -lm

all_win32 clean_win32: SUF=.exe
# name of the binary - only valid for targets which set SYSTEM
DESTPATH = ../bin/$(SYSTEM)/$(Target)$(SUF)

OBJ = $(Sources:.cpp=.o)

all_linux all_win32: $(OBJ)
	$(warning Building...)
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) $^ -o $(DESTPATH) $(LDFLAGS)

clean: clean_linux clean_win32
	$(warning Cleaning...)
	@$(RM) $(OBJ)

clean_linux clean_win32:
	@$(RM) $(DESTPATH)

.PHONY: all all_win32 clean clean_linux clean_win32

INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(LIBBANSHEE_CFLAGS) \
	$(GST_CFLAGS) 

if ENABLE_GAPLESS
INCLUDES += -DENABLE_GAPLESS
endif

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES =  \
	banshee-bpmdetector.c \
	banshee-gst.c \
	banshee-player.c \
	banshee-player-cdda.c \
	banshee-player-equalizer.c \
	banshee-player-missing-elements.c \
	banshee-player-pipeline.c \
	banshee-player-replaygain.c \
	banshee-player-video.c \
	banshee-player-vis.c \
	banshee-ripper.c \
	banshee-tagger.c \
	banshee-transcoder.c

if HAVE_CLUTTER
libbanshee_la_SOURCES += clutter-gst-video-sink.c
INCLUDES += -I$(srcdir)/shaders
else
noinst_DATA = clutter-gst-video-sink.c
endif

noinst_HEADERS =  \
	banshee-gst.h \
	banshee-player-cdda.h \
	banshee-player-equalizer.h \
	banshee-player-missing-elements.h \
	banshee-player-pipeline.h \
	banshee-player-private.h \
	banshee-player-replaygain.h \
	banshee-player-video.h \
	banshee-player-vis.h \
	banshee-tagger.h \
	clutter-gst-shaders.h \
	clutter-gst-video-sink.h \
	shaders/I420.h \
	shaders/YV12.h

libbanshee_la_LIBADD = \
	$(LIBBANSHEE_LIBS) \
	$(GST_LIBS)

all: $(top_builddir)/bin/libbanshee.so

$(top_builddir)/bin/libbanshee.so: libbanshee.la
	mkdir -p $(top_builddir)/bin
	cp -f .libs/libbanshee.so $@

CLEANFILES = $(top_builddir)/bin/libbanshee.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(libbanshee_la_SOURCES)

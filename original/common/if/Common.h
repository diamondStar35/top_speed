/**
* Common library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __COMMON_COMMON_H__
#define __COMMON_COMMON_H__


#include <Common/If/Types.h>
#include <Common/If/Defs.h>

// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the COMMON_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// COMMON_API functions as being imported from a DLL, wheras this DLL sees symbols
// defined with this macro as being exported.
#ifdef COMMON_EXPORTS
#define _common_ __declspec(dllexport)
#else
#define _common_ __declspec(dllimport)
#endif

#ifdef COMMON_STATIC
#undef _common_
#define _common_ 
#endif

#include <Common/If/File.h>
#include <Common/If/Tracer.h>
#include <Common/If/Window.h>
#include <Common/If/Mutex.h>
#include <Common/If/Network.h>


class Tracer;

extern   _common_  Tracer  commonTracer;

#define  COMMON    commonTracer.trace




#endif /* __COMMON_COMMON_H__ */

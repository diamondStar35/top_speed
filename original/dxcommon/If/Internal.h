/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_INTERNAL_H__
#define __DXCOMMON_INTERNAL_H__


#include <Common/If/Common.h>
#include <DxCommon/If/Defs.h>

#include <d3d8.h>


#ifdef DXCOMMON_EXPORTS
#define _dxcommon_ __declspec(dllexport)
#else
#define _dxcommon_ __declspec(dllimport)
#endif


#ifdef DXCOMMON_STATIC
#undef _dxcommon_
#define _dxcommon_ 
#endif


extern   _dxcommon_  Tracer  dxTracer;

#define  DXCOMMON    dxTracer.trace

namespace DirectX
{

struct Vector2
{
    Float   x;
    Float   y;
};

struct Vector3
{
    explicit Vector3( )                               {                      }
    explicit Vector3(Float a, Float b, Float c)       { x = a, y = b, z = c; }

    Float   x;
    Float   y;
    Float   z;

    _dxcommon_ Float lengthSquare( ) const  { return x*x + y*y + z*z; };

    _dxcommon_ Vector3& operator *= (Float f) { x*=f, y*=f, z*=f; return *this; }
    _dxcommon_ Vector3& operator += (Vector3& v) { x+=v.x, y+=v.y, z+=v.z; return *this; }
    _dxcommon_ Vector3  operator + (const Vector3& v) const
                                              {   return Vector3(x + v.x, y + v.y, z + v.z); }
    _dxcommon_ Vector3  operator - (const Vector3& v) const
                                              {   return Vector3(x - v.x, y - v.y, z - v.z); }
    _dxcommon_ Vector3  operator * (const Float& f)  { return Vector3(x*f, y*f, z*f);               }
    
};


class DeviceDependent
{
public:
    _dxcommon_ virtual Int initDevices(/* LPDIRECT3DDEVICE8 d3dDevice */)    { return dxSuccess; }
    _dxcommon_ virtual Int restoreDevices(/* LPDIRECT3DDEVICE8 d3dDevice */) { return dxSuccess; }
    _dxcommon_ virtual Int invalidateDevices( )                        { return dxSuccess; }
    _dxcommon_ virtual Int deleteDevices( )                            { return dxSuccess; }
public:
    DeviceDependent*      next;
    DeviceDependent*      previous;
};

} // namespace DirectX


#endif /* __DXCOMMON_INTERNAL_H__ */

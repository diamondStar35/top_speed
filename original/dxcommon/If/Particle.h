/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_PARTICLE_H__
#define __DXCOMMON_PARTICLE_H__

#include <DxCommon/If/Internal.h>
#include <DxCommon/If/Game.h>

#include <D3D.h>
#include <D3D8types.h>

namespace DirectX
{


struct Particle
{
    enum Type
    {
        fadingSingle    
    };
    Particle( )        {     }

    Vector3     position;       // Current position
    Vector3     velocity;       // Current velocity

    Vector3     initPosition;      // Initial position
    Vector3     initVelocity;      // Initial velocity
    Float       initTime;     // Time of creation

    UInt        color;         // Initial diffuse color
    UInt        fadedColor;   // Faded diffuse color
    Float       fadeProgression;      // Fade progression
    
    Type        type;

    Particle*   next;      // Next particle in list
    Particle*   prev;      // Previous particle in list
};


class ParticleSystem : 
    public DeviceDependent
{
public:
    _dxcommon_ ParticleSystem(LPDIRECT3DDEVICE8 d3dDevice, UInt nparticles, 
                              D3DFORMAT textureFormat, Char* textureFile, Game* game = 0);
    _dxcommon_ virtual ~ParticleSystem();

    _dxcommon_ void emitFadingSingle(Vector3 position, UInt color, Float sceneTime);

    _dxcommon_ void prepareNextFrame(Float sceneTime, Float elapsed);

   // DeviceDependent methods
    _dxcommon_ virtual Int restoreDevices(LPDIRECT3DDEVICE8 d3dDevice);
    _dxcommon_ virtual Int invalidateDevices( );

/*
    _dxcommon_ void update(float secsPerFrame, UInt nParticlesToEmit,
                           const D3DXCOLOR &emitColor, const D3DXCOLOR &dwFadeColor,
                           FLOAT fEmitVel, D3DXVECTOR3 vPosition );
*/
    _dxcommon_ Int render(/* Float sceneTime, Float elapsed */);

protected:
    UInt      m_base;
	UInt      m_flush;
    UInt      m_discard;

    UInt      m_nParticles;
    UInt      m_nMaxParticles;
    Particle* m_particles;
    Particle* m_particlesFree;

    Game*     m_game;

    LPDIRECT3DDEVICE8       m_d3dDevice;
    LPDIRECT3DVERTEXBUFFER8 m_vertexBuffer;
    LPDIRECT3DTEXTURE8      m_texture;
};




}; // namespace DirectX








#endif /* __DXCOMMON_PARTICLE_H__ */

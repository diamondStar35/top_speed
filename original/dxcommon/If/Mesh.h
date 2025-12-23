/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_MESH_H__
#define __DXCOMMON_MESH_H__

#include <DxCommon/If/Common.h>

#include <tchar.h>
#include <d3d8.h>
#include <d3dx8.h>



namespace DirectX
{

struct DEFAULTVERTEX
{
    D3DXVECTOR3 p;
    D3DXVECTOR3 n;
    FLOAT       tu, tv;
};

/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
class Mesh : public DeviceDependent
{
public:
    _dxcommon_ Mesh(Char* name = "DirectX::Mesh");
    _dxcommon_ virtual ~Mesh();

public:
    // Rendering
    _dxcommon_ Int render(LPDIRECT3DDEVICE8 d3dDevice, 
                          Boolean drawOpaqueSubsets = true,
                          Boolean drawAlphaSubsets = true,
                          Float scale = 1.0f);
    _dxcommon_ Int renderAt(LPDIRECT3DDEVICE8 d3dDevice,
                            Float x, Float y, Float z,
                            Float rx = 0, Float ry = 0, Float rz = 0,
                            Boolean drawOpaqueSubsets = true,
                            Boolean drawAlphaSubsets = true,
                            Float scale = 1.0f);
    _dxcommon_ Int renderAt(LPDIRECT3DDEVICE8 d3dDevice,
                            Vector3 pos,
                            Boolean drawOpaqueSubsets = true,
                            Boolean drawAlphaSubsets = true);
    _dxcommon_ Int renderAt(LPDIRECT3DDEVICE8 d3dDevice,
                            Vector3 pos,
                            Vector3 att,
                            Boolean drawOpaqueSubsets = true,
                            Boolean drawAlphaSubsets = true);
    _dxcommon_ Int renderAtCurrent(LPDIRECT3DDEVICE8 d3dDevice, 
                          Boolean drawOpaqueSubsets = true,
                          Boolean drawAlphaSubsets = true);

    _dxcommon_ Int renderShadowAtCurrent(LPDIRECT3DDEVICE8 d3dDevice, Vector3& light, Vector3& u, Vector3& v, Vector3& w);
    _dxcommon_ Int renderShadowAt(LPDIRECT3DDEVICE8 d3dDevice, Vector3& pos, Vector3& att,
                                  Vector3& light, Vector3& u, Vector3& v, Vector3& w);

    _dxcommon_ Int  setFVF(LPDIRECT3DDEVICE8 d3dDevice, UInt FVF);

    // Initializing
    _dxcommon_ virtual Int restoreDevices(LPDIRECT3DDEVICE8 d3dDevice);
    _dxcommon_ virtual Int invalidateDevices( ); 

    // Creation/destruction
    _dxcommon_ Int create(LPDIRECT3DDEVICE8 d3dDevice, Char* filename);
    _dxcommon_ Int create(LPDIRECT3DDEVICE8 d3dDevice, Char* filename, Char* textureFile);
    _dxcommon_ Int create(LPDIRECT3DDEVICE8 d3dDevice, LPDIRECTXFILEDATA fileData);
    _dxcommon_ Int destroy( );

    _dxcommon_ Int setTexture(LPDIRECT3DDEVICE8 d3dDevice, Char* filename);
    
    // 'Get' methods
    _dxcommon_ LPD3DXMESH    systemMesh( ) const     { return m_sysMemMesh; }
    _dxcommon_ LPD3DXMESH    localMesh( ) const      { return m_localMesh;  }
    _dxcommon_ LPDIRECT3DTEXTURE8* textures( ) const { return m_textures;   }
    _dxcommon_ D3DMATERIAL8* materials( ) const      { return m_materials;  }
    _dxcommon_ UInt          nMaterials( ) const     { return m_nMaterials; }

public:
    Char                m_name[512];

    LPD3DXMESH          m_sysMemMesh;    // SysMem mesh, lives through resize
    LPD3DXMESH          m_localMesh;     // Local mesh, rebuilt on resize
    
    UInt                m_nMaterials; // Materials for the mesh
    D3DMATERIAL8*       m_materials;
    LPDIRECT3DTEXTURE8* m_textures;
    Boolean             m_useMaterials;
};




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
class Frame : public DeviceDependent
{
public:
    _dxcommon_ Frame(Char* name = "DirectX::Frame");
    _dxcommon_ virtual ~Frame();

public:
    // Matrix access
    _dxcommon_ void        matrix( D3DXMATRIX* mat )  { m_matrix = *mat;  }
    _dxcommon_ D3DXMATRIX* matrix( )                  { return &m_matrix; }

    _dxcommon_ Mesh*   findMesh(Char* meshName);
    _dxcommon_ Frame*  findFrame(Char* frameName);
    _dxcommon_ Boolean enumMeshes(Boolean (*enumMeshCallback)(Mesh*, void*), void* context);

    _dxcommon_ Int destroy();

    _dxcommon_ virtual Int restoreDevices(LPDIRECT3DDEVICE8 d3dDevice);
    _dxcommon_ virtual Int invalidateDevices( ); 

    _dxcommon_ Int render(LPDIRECT3DDEVICE8 d3dDevice, 
                          Boolean drawOpaqueSubsets = true,
                          Boolean drawAlphaSubsets = true,
                          D3DXMATRIX* matWorldMartix = 0);
    

public:
    Char       m_name[512];
    D3DXMATRIX m_matrix;
    Mesh*      m_mesh;
    Char       m_meshName[512];

    Frame*     m_next;
    Frame*     m_child;
};




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
class MeshFile : public Frame
{
    _dxcommon_ Int loadMesh(LPDIRECT3DDEVICE8 d3dDevice, LPDIRECTXFILEDATA fileData, 
                            Frame* parentFrame);
    _dxcommon_ Int loadFrame(LPDIRECT3DDEVICE8 d3dDevice, LPDIRECTXFILEDATA fileData, 
                             Frame* parentFrame);
public:
    _dxcommon_ Int create(LPDIRECT3DDEVICE8 d3dDevice, Char* filename);
    _dxcommon_ Int createFromResource(LPDIRECT3DDEVICE8 d3dDevice, Char* resource, Char* type);
    // For pure devices, specify the world transform. If the world transform is not
    // specified on pure devices, this function will fail.
    _dxcommon_ Int render(LPDIRECT3DDEVICE8 d3dDevice, D3DXMATRIX* matWorldMatrix = NULL);

    MeshFile( ) : Frame("MeshFile")   { }
};






} // namespace DirectX



#endif /* __DXCOMMON_MESH_H__ */

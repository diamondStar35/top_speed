/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <DxCommon/If/Common.h>

//-----------------------------------------------------------------------------
// File: D3DFile.cpp
//
// Desc: Support code for loading DirectX .X files.
//
// Copyright (c) 1997-2001 Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#include <tchar.h>
#include <stdio.h>
#include <d3d8.h>
#include <d3dx8.h>
#include <dxfile.h>
#include <rmxfguid.h>
#include <rmxftmpl.h>


namespace DirectX
{




#define D3DFVF_DEFAULTVERTEX  (D3DFVF_XYZ|D3DFVF_NORMAL|D3DFVF_TEX1)

/*
HRESULT DXUtil_FindMediaFile(Char* strPath, Char* strFilename )
{
    HANDLE file;
    TCHAR strFullPath[1024];
    TCHAR *strShortName;
    DWORD cchPath;

    if( NULL==strFilename || NULL==strPath )
        return E_INVALIDARG;

    // Build full path name from strFileName (strShortName will be just the leaf filename)
    cchPath = GetFullPathName(strFilename, sizeof(strFullPath)/sizeof(TCHAR), strFullPath, &strShortName);
    if ((cchPath == 0) || (sizeof(strFullPath)/sizeof(TCHAR) <= cchPath))
        return E_FAIL;

    // first try to find the filename given a full path
    file = CreateFile( strFullPath, GENERIC_READ, FILE_SHARE_READ, NULL, 
                       OPEN_EXISTING, 0, NULL );
    if( INVALID_HANDLE_VALUE != file )
    {
        _tcscpy( strPath, strFullPath );
        CloseHandle( file );
        return S_OK;
    }
    
    // next try to find the filename in the current working directory (path stripped)
    file = CreateFile( strShortName, GENERIC_READ, FILE_SHARE_READ, NULL, 
                       OPEN_EXISTING, 0, NULL );
    if( INVALID_HANDLE_VALUE != file )
    {
        _tcscpy( strPath, strShortName );
        CloseHandle( file );
        return S_OK;
    }
    
    // On failure, just return the file as the path
    _tcscpy( strPath, strFilename );
    return E_FAIL;
}
*/



/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Mesh::Mesh(Char* name )
{
    _tcscpy( m_name, name );
    m_sysMemMesh        = 0;
    m_localMesh         = 0;
    m_nMaterials        = 0;
    m_materials         = 0;
    m_textures         = 0;
    m_useMaterials      = true;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Mesh::~Mesh()
{
    destroy();
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::create(LPDIRECT3DDEVICE8 d3dDevice, Char* filename) 
{
    Char         path[MAX_PATH];
    LPD3DXBUFFER adjacencyBuffer = 0;
    LPD3DXBUFFER materialBuffer  = 0;
   
    // Find the path for the file, and convert it to ANSI (for the D3DX API)
    findFile(path, filename);
    
    // Load the mesh
    if (FAILED(D3DXLoadMeshFromX(path, D3DXMESH_SYSTEMMEM, d3dDevice, 
                                 &adjacencyBuffer, &materialBuffer, 
                                 (DWORD*) &m_nMaterials, &m_sysMemMesh)))
    {
        DXCOMMON("(!) Mesh::create : failed to load mesh from file '%s'.", filename);
        return dxFailed;
    }

    // Optimize the mesh for performance
    if (FAILED(m_sysMemMesh->OptimizeInplace(
                        D3DXMESHOPT_COMPACT | D3DXMESHOPT_ATTRSORT | D3DXMESHOPT_VERTEXCACHE,
                        (DWORD*)adjacencyBuffer->GetBufferPointer(), NULL, NULL, NULL)))
    {
        DXCOMMON("(!) Mesh::create : failed to optimize mesh (file '%s').", filename);
        SAFE_RELEASE(adjacencyBuffer );
        SAFE_RELEASE(materialBuffer );
        return dxFailed;
    }

    // Get material info for the mesh
    // Get the array of materials out of the buffer
    if (materialBuffer && m_nMaterials > 0)
    {
        // Allocate memory for the materials and textures
        D3DXMATERIAL* materials = (D3DXMATERIAL*)materialBuffer->GetBufferPointer();
        m_materials = new D3DMATERIAL8[m_nMaterials];
        m_textures  = new LPDIRECT3DTEXTURE8[m_nMaterials];

        // Copy each material and create its texture
        for (UInt i=0; i < m_nMaterials; i++)
        {
            // Copy the material
            m_materials[i]         = materials[i].MatD3D;
            m_materials[i].Ambient = m_materials[i].Diffuse;
            m_textures[i]          = 0;

            // Create a texture
            if (materials[i].pTextureFilename)
            {
                Char texture[MAX_PATH];
                if (findFile(texture, materials[i].pTextureFilename) != dxSuccess)
                {
                    TCHAR  strTexturePath[MAX_PATH];
                    LPTSTR strFilePart;
                    GetFullPathName(filename, MAX_PATH, strTexturePath, &strFilePart);
                    strcpy(strFilePart, materials[i].pTextureFilename);
                    if (findFile(texture, strTexturePath) != dxSuccess)
                    {
                        DXCOMMON("(!) Failed to locate texture %s", materials[i].pTextureFilename); 
                    }
                }
     
                if (FAILED(D3DXCreateTextureFromFile(d3dDevice, texture, 
                                                     &m_textures[i])))
                    m_textures[i] = 0;
            }
        }
    }

    SAFE_RELEASE( adjacencyBuffer );
    SAFE_RELEASE( materialBuffer );

    //setFVF(d3dDevice, D3DFVF_DEFAULTVERTEX); 

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::create(LPDIRECT3DDEVICE8 d3dDevice, Char* filename, Char* textureFile) 
{
    Char         path[MAX_PATH];
    LPD3DXBUFFER adjacencyBuffer = 0;
    LPD3DXBUFFER materialBuffer  = 0;
   
    // Find the path for the file, and convert it to ANSI (for the D3DX API)
    findFile(path, filename);
    
    // Load the mesh
    if (FAILED(D3DXLoadMeshFromX(path, D3DXMESH_SYSTEMMEM, d3dDevice, 
                                 &adjacencyBuffer, &materialBuffer, 
                                 (DWORD*) &m_nMaterials, &m_sysMemMesh)))
    {
        DXCOMMON("(!) Mesh::create : failed to load mesh from file '%s'.", filename);
        return dxFailed;
    }

    // Optimize the mesh for performance
    if (FAILED(m_sysMemMesh->OptimizeInplace(
                        D3DXMESHOPT_COMPACT | D3DXMESHOPT_ATTRSORT | D3DXMESHOPT_VERTEXCACHE,
                        (DWORD*)adjacencyBuffer->GetBufferPointer(), NULL, NULL, NULL)))
    {
        DXCOMMON("(!) Mesh::create : failed to optimize mesh (file '%s').", filename);
        SAFE_RELEASE(adjacencyBuffer );
        SAFE_RELEASE(materialBuffer );
        return dxFailed;
    }

    // Get material info for the mesh
    // Get the array of materials out of the buffer
    if (materialBuffer && m_nMaterials > 0)
    {
        // Allocate memory for the materials and textures
        D3DXMATERIAL* materials = (D3DXMATERIAL*)materialBuffer->GetBufferPointer();
        m_materials = new D3DMATERIAL8[m_nMaterials];
        m_textures  = new LPDIRECT3DTEXTURE8[m_nMaterials];

        // Copy each material and create its texture
        for (UInt i=0; i < m_nMaterials; i++)
        {
            // Copy the material
            m_materials[i]         = materials[i].MatD3D;
            m_materials[i].Ambient = m_materials[i].Diffuse;
            m_textures[i]          = 0;

            // Create a texture
            if (materials[i].pTextureFilename)
            {
                Char texture[MAX_PATH];
                findFile(texture, textureFile);

                if (FAILED(D3DXCreateTextureFromFile(d3dDevice, texture, 
                                                     &m_textures[i])))
                    m_textures[i] = 0;
            }
        }
    }

    SAFE_RELEASE( adjacencyBuffer );
    SAFE_RELEASE( materialBuffer );

    //setFVF(d3dDevice, D3DFVF_DEFAULTVERTEX); 

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::create(LPDIRECT3DDEVICE8 d3dDevice, LPDIRECTXFILEDATA fileData )
{
    LPD3DXBUFFER materialBuffer = 0;
    LPD3DXBUFFER adjacencyBuffer = 0;
    
    // Load the mesh from the DXFILEDATA object
    if (FAILED(D3DXLoadMeshFromXof(fileData, D3DXMESH_SYSTEMMEM, d3dDevice,
                                   &adjacencyBuffer, &materialBuffer, 
                                   (DWORD*) &m_nMaterials, &m_sysMemMesh)))
    {
        DXCOMMON("(!) Mesh::create : failed to load mesh from filedata.");
        return dxFailed;
    }

    // Optimize the mesh for performance
    if (FAILED(m_sysMemMesh->OptimizeInplace(
                        D3DXMESHOPT_COMPACT | D3DXMESHOPT_ATTRSORT | D3DXMESHOPT_VERTEXCACHE,
                        (DWORD*)adjacencyBuffer->GetBufferPointer(), NULL, NULL, NULL)))
    {
        DXCOMMON("(!) Mesh::create : failed to optimize mesh.");
        SAFE_RELEASE(adjacencyBuffer );
        SAFE_RELEASE(materialBuffer );
        return dxFailed;
    }

    // Get material info for the mesh
    // Get the array of materials out of the buffer
    if (materialBuffer && m_nMaterials > 0)
    {
        // Allocate memory for the materials and textures
        D3DXMATERIAL* materials = (D3DXMATERIAL*)materialBuffer->GetBufferPointer();
        m_materials = new D3DMATERIAL8[m_nMaterials];
        m_textures  = new LPDIRECT3DTEXTURE8[m_nMaterials];

        // Copy each material and create its texture
        for (UInt i=0; i < m_nMaterials; i++)
        {
            // Copy the material
            m_materials[i]         = materials[i].MatD3D;
            m_materials[i].Ambient = m_materials[i].Diffuse;
            m_textures[i]          = 0;

            // Create a texture
            if (materials[i].pTextureFilename )
            {
                Char  texture[MAX_PATH];
                findFile(texture, materials[i].pTextureFilename);

                if (FAILED(D3DXCreateTextureFromFile(d3dDevice, texture, 
                                                     &m_textures[i])))
                    m_textures[i] = 0;
            }
        }
    }

    SAFE_RELEASE(adjacencyBuffer);
    SAFE_RELEASE(materialBuffer);

    return dxSuccess;
}


Int Mesh::setTexture(LPDIRECT3DDEVICE8 d3dDevice, Char* filename)
{
    LPDIRECT3DTEXTURE8* tempTextures = new LPDIRECT3DTEXTURE8[m_nMaterials];

    // Copy each material and create its texture
    for (UInt i=0; i < m_nMaterials; i++)
    {
        if (m_textures[i])
        {
            Char texture[MAX_PATH];
            findFile(texture, filename);

            if (FAILED(D3DXCreateTextureFromFile(d3dDevice, texture, 
                                                 &tempTextures[i])))
/*            if (FAILED(D3DXCreateTextureFromFileEx(d3dDevice,
                                                   texture,
  D3DX_DEFAULT,
  D3DX_DEFAULT,
  D3DX_DEFAULT,
  D3DUSAGE_RENDERTARGET,
  D3DFMT_UNKNOWN,
  D3DPOOL_DEFAULT,
  D3DX_DEFAULT,
  D3DX_DEFAULT ,
  0x00ffffff,
  0,
  0,
  &tempTextures[i])))
*/
                tempTextures[i] = 0;
        }
        else
            tempTextures[i] = 0;
    }

    SAFE_DELETE_ARRAY(m_textures);
    m_textures = tempTextures;
    return dxSuccess;
}



/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::setFVF(LPDIRECT3DDEVICE8 d3dDevice, UInt FVF)
{
    LPD3DXMESH tempSysMemMesh = 0;
    LPD3DXMESH tempLocalMesh  = 0;

    if (m_sysMemMesh)
    {
        if (FAILED(m_sysMemMesh->CloneMeshFVF(D3DXMESH_SYSTEMMEM, FVF,
                                              d3dDevice, &tempSysMemMesh)))
        {
            DXCOMMON("(!) Mesh::setFVF : failed to clone FVF.");
            return dxFailed;
        }
    }
    if (m_localMesh)
    {
        if (FAILED(m_localMesh->CloneMeshFVF(0L, FVF, d3dDevice,
                                                &tempLocalMesh)))
        {
            DXCOMMON("(!) Mesh::setFVF : failed to clone FVF.");
            SAFE_RELEASE(tempSysMemMesh);
            return dxFailed;
        }
    }

    SAFE_RELEASE(m_sysMemMesh);
    SAFE_RELEASE(m_localMesh);

    if (tempSysMemMesh) m_sysMemMesh = tempSysMemMesh;
    if (tempLocalMesh)  m_localMesh  = tempLocalMesh;

    // Compute normals in case the meshes have them
    if (m_sysMemMesh)
        D3DXComputeNormals(m_sysMemMesh, NULL);
    if (m_localMesh)
        D3DXComputeNormals(m_localMesh, NULL);

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::restoreDevices(LPDIRECT3DDEVICE8 d3dDevice)
{
    if (m_sysMemMesh == 0)
        return dxFailed;

    // Make a local memory version of the mesh. Note: because we are passing in
    // no flags, the default behavior is to clone into local memory.
    if (FAILED(m_sysMemMesh->CloneMeshFVF( 0L, m_sysMemMesh->GetFVF(),
                                           d3dDevice, &m_localMesh)))
    {
        DXCOMMON("(!) Mesh::restoreDevices : failed to clone FVF.");
        return dxFailed;
    }

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::invalidateDevices( )
{
    SAFE_RELEASE(m_localMesh);
    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::destroy()
{
    invalidateDevices();
    for (UInt i = 0; i < m_nMaterials; ++i)
        SAFE_RELEASE(m_textures[i]);
    SAFE_DELETE_ARRAY(m_textures);
    SAFE_DELETE(m_materials);

    SAFE_RELEASE(m_sysMemMesh);

    m_nMaterials = 0;

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::render(LPDIRECT3DDEVICE8 d3dDevice, Boolean drawOpaqueSubsets,
                 Boolean drawAlphaSubsets, Float scale)
{
    D3DXMATRIX mat1;
    D3DXMatrixScaling(&mat1, scale, scale, scale);
    d3dDevice->SetTransform(D3DTS_WORLD , &mat1);

    if (m_localMesh == 0)
        return dxSuccess;

    // Frist, draw the subsets without alpha
    if (drawOpaqueSubsets)
    {
        for (UInt i = 0; i < m_nMaterials; ++i)
        {
            if (m_useMaterials)
            {
                if (m_materials[i].Diffuse.a < 1.0f)
                    continue;
                d3dDevice->SetMaterial(&m_materials[i]);
                d3dDevice->SetTexture(0, m_textures[i]);
            }
            m_localMesh->DrawSubset( i );
        }
    }

    // Then, draw the subsets with alpha
    if (drawAlphaSubsets && m_useMaterials)
    {
        for (UInt i = 0; i < m_nMaterials; ++i)
        {
            if (m_materials[i].Diffuse.a == 1.0f)
                continue;

            // Enable alpha blending
            d3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
            d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_SRCALPHA);
            d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);

            // Set the material and texture
            d3dDevice->SetMaterial(&m_materials[i]);
            d3dDevice->SetTexture(0, m_textures[i]);
            m_localMesh->DrawSubset(i);
        }
    }

    return dxSuccess;
}



/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::renderAt(LPDIRECT3DDEVICE8 d3dDevice,
                            Float x, Float y, Float z,
                            Float rx, Float ry, Float rz,
                            Boolean drawOpaqueSubsets,
                            Boolean drawAlphaSubsets,
                            Float scale)
{
    D3DXMATRIX mat1, mat2, mat3, mat4;
    D3DXMatrixScaling(&mat1, scale, scale, scale);
    D3DXMatrixRotationYawPitchRoll(&mat2, rx, ry, rz);
    D3DXMatrixMultiply(&mat3, &mat1, &mat2);
    D3DXMatrixTranslation(&mat1, x, y, z);
	D3DXMatrixMultiply(&mat4, &mat3, &mat1);
    d3dDevice->SetTransform(D3DTS_WORLD , &mat4);


    if (m_localMesh == 0)
        return dxSuccess;

    // Frist, draw the subsets without alpha
    if (drawOpaqueSubsets)
    {
        for (UInt i = 0; i < m_nMaterials; ++i)
        {
            if (m_useMaterials)
            {
                if (m_materials[i].Diffuse.a < 1.0f)
                    continue;
                d3dDevice->SetMaterial(&m_materials[i]);
                d3dDevice->SetTexture(0, m_textures[i]);
            }
            m_localMesh->DrawSubset( i );
        }
    }

    // Then, draw the subsets with alpha
    if (drawAlphaSubsets && m_useMaterials)
    {
        for (UInt i = 0; i < m_nMaterials; ++i)
        {
            if (m_materials[i].Diffuse.a == 1.0f)
                continue;

            // Enable alpha blending
            d3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
            d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_SRCALPHA);
            d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);

            // Set the material and texture
            d3dDevice->SetMaterial(&m_materials[i]);
            d3dDevice->SetTexture(0, m_textures[i]);
            m_localMesh->DrawSubset(i);
        }
    }

    D3DXMatrixIdentity(&mat1);
    d3dDevice->SetTransform(D3DTS_WORLD , &mat1);
    
    return dxSuccess;
}


/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::renderAt(LPDIRECT3DDEVICE8 d3dDevice, Vector3 pos,
                            Boolean drawOpaqueSubsets,
                            Boolean drawAlphaSubsets)
{
    return renderAt(d3dDevice, pos.x, pos.y, pos.z, 0, 0, 0, drawOpaqueSubsets, drawAlphaSubsets);
}


/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::renderAt(LPDIRECT3DDEVICE8 d3dDevice, Vector3 pos, Vector3 att,
                            Boolean drawOpaqueSubsets,
                            Boolean drawAlphaSubsets)
{
    return renderAt(d3dDevice, pos.x, pos.y, pos.z, att.x, att.y, att.z, drawOpaqueSubsets, drawAlphaSubsets);
}
    



/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Mesh::renderAtCurrent(LPDIRECT3DDEVICE8 d3dDevice, 
                          Boolean drawOpaqueSubsets,
                          Boolean drawAlphaSubsets)
{
    if (m_localMesh == 0)
        return dxSuccess;

    // Frist, draw the subsets without alpha
    if (drawOpaqueSubsets)
    {
        for (UInt i = 0; i < m_nMaterials; ++i)
        {
            if (m_useMaterials)
            {
                if (m_materials[i].Diffuse.a < 1.0f)
                    continue;
                d3dDevice->SetMaterial(&m_materials[i]);
                d3dDevice->SetTexture(0, m_textures[i]);
            }
            m_localMesh->DrawSubset( i );
        }
    }

    // Then, draw the subsets with alpha
    if (drawAlphaSubsets && m_useMaterials)
    {
        for (UInt i = 0; i < m_nMaterials; ++i)
        {
            if (m_materials[i].Diffuse.a == 1.0f)
                continue;

            // Enable alpha blending
            d3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
            d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_SRCALPHA);
            d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);

            // Set the material and texture
            d3dDevice->SetMaterial(&m_materials[i]);
            d3dDevice->SetTexture(0, m_textures[i]);
            m_localMesh->DrawSubset(i);
        }
    }

    return dxSuccess;
}


Int Mesh::renderShadowAtCurrent(LPDIRECT3DDEVICE8 d3dDevice, Vector3& light, Vector3& u, Vector3& v, Vector3& w)
{
    // Poll for current renderstate to reinitialize after rendering
    unsigned long lighting;
    d3dDevice->GetRenderState(D3DRS_LIGHTING, &lighting);
    // Set the correct renderstates
    //d3dDevice->SetRenderState(D3DRS_LIGHTING, FALSE);
    d3dDevice->SetRenderState(D3DRS_ZWRITEENABLE, FALSE);
    // Calculate the plane-equation coefficients
    Float a = ((u.y - v.y)*(u.z - w.z) - (u.y - w.y)*(u.z - v.z));
    Float b = ((u.x - w.x)*(u.z - v.z) - (u.x - v.x)*(u.z - w.z));
    Float c = ((u.x - v.x)*(u.y - w.y) - (u.y - v.y)*(u.x - w.x));
    Float d = -u.x*a - (u.y+0.002f)*b - u.z*c;
    D3DXPLANE plane(a, b, c, d);

    D3DXMATRIX shadowMatrix;
    D3DXMATRIX worldMatrix;
    D3DXVECTOR4 light4D = D3DXVECTOR4(light.x, light.y, light.z, 0);
    D3DXMatrixShadow(&shadowMatrix, &light4D, &plane);
    D3DXMATRIX mat1, mat2;
    d3dDevice->GetTransform(D3DTS_WORLDMATRIX(0), &mat1);
    D3DXMatrixMultiply(&mat2, &mat1, &shadowMatrix);
    d3dDevice->SetTransform(D3DTS_WORLD, &mat2);
        
    // Set the material and texture
    
    for (UInt i = 0; i < m_nMaterials; ++i)
    {
        // Enable alpha blending
        d3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
        //d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_INVSRCCOLOR);
        d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_ONE);
        //d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_SRCCOLOR);
        d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_ZERO);

        D3DMATERIAL8 mtrl;
        UInt color = (UInt)Color::black;
        ZeroMemory( &mtrl, sizeof(D3DMATERIAL8) );
        mtrl.Diffuse.r = mtrl.Ambient.r = Float((color >> 16) & 0xff)/255.0f;
        mtrl.Diffuse.g = mtrl.Ambient.g = Float((color >> 8) & 0xff)/255.0f;
        mtrl.Diffuse.b = mtrl.Ambient.b = Float(color & 0xff)/255.0f;
        mtrl.Diffuse.a = mtrl.Ambient.a = 0.5f;
                
        d3dDevice->SetMaterial(&mtrl);    
        
        //d3dDevice->SetMaterial(&m_materials[i]);
        //d3dDevice->SetTexture(0, m_textures[i]);
        m_localMesh->DrawSubset(i);
        
    }  

    // reset original renderstates
    d3dDevice->SetRenderState(D3DRS_LIGHTING, lighting);
    d3dDevice->SetRenderState(D3DRS_ZWRITEENABLE, TRUE);
    return dxSuccess;
}


Int Mesh::renderShadowAt(LPDIRECT3DDEVICE8 d3dDevice, Vector3& pos, Vector3& att,
                         Vector3& light, Vector3& u, Vector3& v, Vector3& w)
{
    // Poll for current renderstate to reinitialize after rendering
    unsigned long lighting;
    d3dDevice->GetRenderState(D3DRS_LIGHTING, &lighting);
    // Set the correct renderstates
    //d3dDevice->SetRenderState(D3DRS_LIGHTING, FALSE);
    d3dDevice->SetRenderState(D3DRS_ZWRITEENABLE, FALSE);
    // Calculate the plane-equation coefficients
    u.y += 0.001f;
    v.y += 0.001f;
    w.y += 0.001f;
    Float a = ((u.y - v.y)*(u.z - w.z) - (u.y - w.y)*(u.z - v.z));
    Float b = ((u.x - w.x)*(u.z - v.z) - (u.x - v.x)*(u.z - w.z));
    Float c = ((u.x - v.x)*(u.y - w.y) - (u.y - v.y)*(u.x - w.x));
    Float d = -u.x*a - u.y*b - u.z*c;
    D3DXPLANE plane(a, b, c, d);

    D3DXMATRIX shadowMatrix;
    D3DXMATRIX worldMatrix;
    D3DXVECTOR4 light4D = D3DXVECTOR4(light.x, light.y, light.z, 1);
    D3DXMatrixShadow(&shadowMatrix, &light4D, &plane);
    D3DXMATRIX mat1, mat2, mat3, mat4;
    D3DXMatrixIdentity(&mat1);
    d3dDevice->SetTransform(D3DTS_WORLD, &mat1);
    D3DXMatrixScaling(&mat1, 1.0f, 1.0f, 1.0f);
    D3DXMatrixRotationYawPitchRoll(&mat2, att.x, att.y, att.z);
    D3DXMatrixMultiply(&mat3, &mat1, &mat2);
    D3DXMatrixTranslation(&mat1, pos.x, pos.y, pos.z);
    D3DXMatrixMultiply(&mat4, &mat3, &mat1);
    D3DXMatrixMultiply(&mat1, &mat4, &shadowMatrix);
    d3dDevice->SetTransform(D3DTS_WORLD, &mat1);
        
    // Set the material and texture
    
    for (UInt i = 0; i < m_nMaterials; ++i)
    {
        // Enable alpha blending
        d3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
        //d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_INVSRCCOLOR);
        d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_ONE);
        //d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_SRCCOLOR);
        d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_ZERO);

        D3DMATERIAL8 mtrl;
        UInt color = (UInt)Color::black;
        ZeroMemory( &mtrl, sizeof(D3DMATERIAL8) );
        mtrl.Diffuse.r = mtrl.Ambient.r = Float((color >> 16) & 0xff)/255.0f;
        mtrl.Diffuse.g = mtrl.Ambient.g = Float((color >> 8) & 0xff)/255.0f;
        mtrl.Diffuse.b = mtrl.Ambient.b = Float(color & 0xff)/255.0f;
        mtrl.Diffuse.a = mtrl.Ambient.a = 0.5f;
                
        d3dDevice->SetMaterial(&mtrl);    
        
        //d3dDevice->SetMaterial(&m_materials[i]);
        //d3dDevice->SetTexture(0, m_textures[i]);
        m_localMesh->DrawSubset(i);
        
    }  

    // reset original renderstates
    d3dDevice->SetRenderState(D3DRS_LIGHTING, lighting);
    d3dDevice->SetRenderState(D3DRS_ZWRITEENABLE, TRUE);
    return dxSuccess;
}


/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Frame::Frame(Char* name) :
    m_mesh(0),
    m_child(0),
    m_next(0)
{
    // Initialize all members
    _tcscpy( m_name, name );
    D3DXMatrixIdentity(&m_matrix);
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Frame::~Frame()
{
    SAFE_DELETE(m_child);
    SAFE_DELETE(m_next);
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Boolean Frame::enumMeshes(Boolean (*enumMeshCallback)(Mesh*, void*), void* context)
{
    if (m_mesh)
        enumMeshCallback(m_mesh, context);
    if (m_child)
        m_child->enumMeshes(enumMeshCallback, context);
    if (m_next)
        m_next->enumMeshes(enumMeshCallback, context);

    return true;
}


/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Mesh* Frame::findMesh(Char* meshName)
{
    Mesh* mesh;

    if (m_mesh)
        if (!lstrcmpi(m_mesh->m_name, meshName))
            return m_mesh;

    if (m_child)
        if ((mesh = m_child->findMesh(meshName)) != 0)
            return mesh;

    if (m_next)
        if ((mesh = m_next->findMesh(meshName )) != 0)
            return mesh;

    return 0;
}



/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Frame* Frame::findFrame(Char* frameName )
{
    Frame* frame;

    if (!lstrcmpi(m_name, frameName))
        return this;

    if (m_child)
        if ((frame = m_child->findFrame(frameName)) != 0)
            return frame;

    if (m_next)
        if ((frame = m_next->findFrame(frameName)) != 0)
            return frame;

    return 0;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Frame::destroy()
{
    if (m_mesh)  m_mesh->destroy();
    if (m_child) m_child->destroy();
    if (m_next)  m_next->destroy();

    SAFE_DELETE(m_mesh);
    SAFE_DELETE(m_next);
    SAFE_DELETE(m_child);

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Frame::restoreDevices(LPDIRECT3DDEVICE8 d3dDevice)
{
    if (m_mesh)  m_mesh->restoreDevices(d3dDevice);
    if (m_child) m_child->restoreDevices(d3dDevice);
    if (m_next)  m_next->restoreDevices(d3dDevice);
    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Frame::invalidateDevices()
{
    if (m_mesh)  m_mesh->invalidateDevices();
    if (m_child) m_child->invalidateDevices();
    if (m_next)  m_next->invalidateDevices();
    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int Frame::render(LPDIRECT3DDEVICE8 d3dDevice, Boolean drawOpaqueSubsets,
                  Boolean drawAlphaSubsets, D3DXMATRIX* matWorldMatrix)
{
    // For pure devices, specify the world transform. If the world transform is not
    // specified on pure devices, this function will fail.

    D3DXMATRIX matSavedWorld, matWorld;

    if (matWorldMatrix == 0)
        d3dDevice->GetTransform(D3DTS_WORLD, &matSavedWorld);
    else
        matSavedWorld = *matWorldMatrix;

    D3DXMatrixMultiply(&matWorld, &m_matrix, &matSavedWorld);
    d3dDevice->SetTransform(D3DTS_WORLD, &matWorld);

    if (m_mesh)
        m_mesh->render(d3dDevice, drawOpaqueSubsets, drawAlphaSubsets);

    if (m_child)
        m_child->render(d3dDevice, drawOpaqueSubsets, drawAlphaSubsets, &matWorld);

    d3dDevice->SetTransform(D3DTS_WORLD, &matSavedWorld);

    if (m_next)
        m_next->render(d3dDevice, drawOpaqueSubsets, drawAlphaSubsets, &matSavedWorld);

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int MeshFile::loadFrame(LPDIRECT3DDEVICE8 d3dDevice, LPDIRECTXFILEDATA fileData, Frame* parentFrame )
{
    LPDIRECTXFILEDATA   childData    = 0;
    LPDIRECTXFILEOBJECT childObject  = 0;
    const GUID*  guid;
    UInt         size;
    Frame*       currentFrame;
    HRESULT      hr;

    // Get the type of the object
    if (FAILED(fileData->GetType(&guid)))
    {
        DXCOMMON("MeshFile::loadFrame : failed to get type of X-file data");
        return dxFailed;
    }

    if (*guid == TID_D3DRMMesh)
    {
        if (loadMesh(d3dDevice, fileData, parentFrame) != dxSuccess)
        {
            DXCOMMON("MeshFile::loadFrame : failed to load mesh from file.");
            return dxFailed;
        }
    }
    if (*guid == TID_D3DRMFrameTransformMatrix)
    {
        D3DXMATRIX* matMatrix;
        if (FAILED(fileData->GetData(NULL, (DWORD*) &size, (VOID**)&matMatrix)))
        {
            DXCOMMON("MeshFile::loadFrame : failed to get data of X-file.");
            return dxFailed;
        }

        // Update the parent's matrix with the new one
        parentFrame->matrix(matMatrix);
    }
    if (*guid == TID_D3DRMFrame)
    {
        // Get the frame name
        Char  name[MAX_PATH] = "";
        UInt  nameLength;
        fileData->GetName( NULL, (DWORD*) &nameLength );
        if (nameLength > 0)
            fileData->GetName(name, (DWORD*) &nameLength);
        
        // Create the frame
        currentFrame = new Frame(name);

        currentFrame->m_next = parentFrame->m_child;
        parentFrame->m_child = currentFrame;

        // Enumerate child objects
        while (SUCCEEDED(fileData->GetNextObject(&childObject)))
        {
            // Query the child for its FileData
            hr = childObject->QueryInterface(IID_IDirectXFileData,
                                          (void**)&childData);
            if (SUCCEEDED(hr))
            {
                if (loadFrame(d3dDevice, childData, currentFrame) != dxSuccess)
                {
                    childData->Release();
                    DXCOMMON("MeshFile::loadFrame : failed to load frame.");
                    return dxFailed;
                }
                childData->Release();
            }
            else
            {
                childObject->Release();        
                return dxFailed;
            }
            childObject->Release();
        }
    }

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int MeshFile::loadMesh(LPDIRECT3DDEVICE8 d3dDevice, LPDIRECTXFILEDATA fileData, Frame* parentFrame )
{
    // Currently only allowing one mesh per frame
    if (parentFrame->m_mesh)
        return dxFailed;

    // Get the mesh name
    Char  name[MAX_PATH] = "";
    UInt  nameLength;
    fileData->GetName(NULL, (DWORD*) &nameLength);
    if (nameLength > 0)
        fileData->GetName(name, (DWORD*) &nameLength);
    
    // Create the mesh
    parentFrame->m_mesh = new Mesh(name);
    parentFrame->m_mesh->create(d3dDevice, fileData);

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int MeshFile::createFromResource(LPDIRECT3DDEVICE8 d3dDevice, Char* resource, Char* type)
{
    LPDIRECTXFILE           DXFile     = 0;
    LPDIRECTXFILEENUMOBJECT enumObject = 0;
    LPDIRECTXFILEDATA       fileData   = 0;
    Int                     result;

    // Create a x file object
    if (FAILED(DirectXFileCreate(&DXFile)))
    {
        DXCOMMON("MeshFile::createFromResource : failed to create a X file from resource %s.", resource);
        return dxFailed;
    }

    // Register templates for d3drm and patch extensions.
    if (FAILED(DXFile->RegisterTemplates((void*)D3DRM_XTEMPLATES, D3DRM_XTEMPLATE_BYTES)))
    {
        DXFile->Release();
        DXCOMMON("MeshFile::createFromResource : failed to register templates from resource %s.", resource);
        return dxFailed;
    }
    
    DXFILELOADRESOURCE dxlr;
    dxlr.hModule = NULL;
    dxlr.lpName  = resource;
    dxlr.lpType  = (TCHAR*) type;

    // Create enum object
    if (FAILED(DXFile->CreateEnumObject( (VOID*)&dxlr, DXFILELOAD_FROMRESOURCE, &enumObject)))
    {
        DXFile->Release();
        DXCOMMON("MeshFile::createFromResource : failed to create an enumeration object for resource %s.", resource);
        return dxFailed;
    }

    // Enumerate top level objects (which are always frames)
    while (SUCCEEDED(enumObject->GetNextDataObject(&fileData)))
    {
        result = loadFrame(d3dDevice, fileData, this);
        fileData->Release();
        if (result != dxSuccess)
        {
            enumObject->Release();
            DXFile->Release();
            DXCOMMON("MeshFile::createFromResource : failed to load frame for resource %s.", resource);
            return dxFailed;
        }
    }

    SAFE_RELEASE(fileData);
    SAFE_RELEASE(enumObject);
    SAFE_RELEASE(DXFile);

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int MeshFile::create(LPDIRECT3DDEVICE8 d3dDevice, Char* filename)
{
    LPDIRECTXFILE           DXFile     = 0;
    LPDIRECTXFILEENUMOBJECT enumObject = 0;
    LPDIRECTXFILEDATA       fileData   = 0;
    Int                     result;

    // Create a x file object
    if (FAILED(DirectXFileCreate(&DXFile)))
    {
        DXCOMMON("MeshFile::create : failed to create a X file from file %s.", filename);
        return dxFailed;
    }

    // Register templates for d3drm and patch extensions.
    if (FAILED(DXFile->RegisterTemplates((void*)D3DRM_XTEMPLATES,
                                          D3DRM_XTEMPLATE_BYTES)))
    {
        DXFile->Release();
        DXCOMMON("MeshFile::create : failed to register templates from file %s.", filename);
        return dxFailed;
    }

    // Find the path to the file, and convert it to ANSI (for the D3DXOF API)
    Char name[MAX_PATH];
    findFile(name, filename);
        
    // Create enum object
    if (FAILED(DXFile->CreateEnumObject((void*)name, DXFILELOAD_FROMFILE, &enumObject)))
    {
        DXFile->Release();
        DXCOMMON("MeshFile::create : failed to create an enumeration object for file %s.", filename);
        return dxFailed;
    }

    // Enumerate top level objects (which are always frames)
    while (SUCCEEDED(enumObject->GetNextDataObject(&fileData)))
    {
        result = loadFrame(d3dDevice, fileData, this);
        fileData->Release( );
        if (result != dxSuccess)
        {
            enumObject->Release();
            DXFile->Release();
            DXCOMMON("MeshFile::create : failed to load frame for file %s.", filename);
            return dxFailed;
        }
    }

    SAFE_RELEASE(fileData);
    SAFE_RELEASE(enumObject);
    SAFE_RELEASE(DXFile);

    return dxSuccess;
}




/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
Int MeshFile::render(LPDIRECT3DDEVICE8 d3dDevice, D3DXMATRIX* matWorldMatrix)
{

    // For pure devices, specify the world transform. If the world transform is not
    // specified on pure devices, this function will fail.

    // Set up the world transformation
    D3DXMATRIX matSavedWorld, matWorld;

    if (matWorldMatrix == 0)
        d3dDevice->GetTransform(D3DTS_WORLD, &matSavedWorld);
    else
        matSavedWorld = *matWorldMatrix;

    D3DXMatrixMultiply(&matWorld, &matSavedWorld, &m_matrix);
    d3dDevice->SetTransform( D3DTS_WORLD, &matWorld);

    // Render opaque subsets in the meshes
    if (m_child)
        m_child->render(d3dDevice, true, false, &matWorld);

    // Enable alpha blending
    d3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
    d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_SRCALPHA);
    d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);

    // Render alpha subsets in the meshes
    if (m_child)
        m_child->render(d3dDevice, false, true, &matWorld);

    // Restore state
    d3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
    d3dDevice->SetTransform(D3DTS_WORLD, &matSavedWorld);

    return dxSuccess;
}



} // namespace DirectX

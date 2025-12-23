/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <DxCommon/If/Utilities.h>
#include <tchar.h>
#include <D3DX8.h>

namespace DirectX
{

Int findFile(Char* path, Char* filename)
{
    HANDLE file;
    Char fullPath[1024];
    Char *shortName;
    DWORD pathnameSize;

    if (filename == 0)
    {
        DXCOMMON("(!) findFile(%s) : please give filename.", filename);
        return dxFailed;
    }

    pathnameSize = GetFullPathName(filename, sizeof(fullPath)/sizeof(Char), fullPath, &shortName);
    if ((pathnameSize == 0) || (sizeof(fullPath)/sizeof(Char) <= pathnameSize))
    {
        DXCOMMON("(!) findFile(%s, %s) : error getting full pathname.", path, filename);
        return dxFailed;
    }

    file = CreateFile(fullPath, GENERIC_READ, FILE_SHARE_READ, NULL, 
                      OPEN_EXISTING, 0, NULL);
    if (INVALID_HANDLE_VALUE != file)
    {
        _tcscpy(path, fullPath);
        CloseHandle(file);
        return dxSuccess;
    }
    
    file = CreateFile(shortName, GENERIC_READ, FILE_SHARE_READ, NULL, 
                      OPEN_EXISTING, 0, NULL );
    if (INVALID_HANDLE_VALUE != file)
    {
        _tcscpy(path, shortName);
        CloseHandle(file);
        return dxSuccess;
    }
    
    //DXCOMMON("(!) findFile(%s, %s) : unable to locate file.", path, filename);

    _tcscpy(path, filename);
    return dxFailed;
}



Int createTexture(LPDIRECT3DDEVICE8 d3dDevice, D3DFORMAT textureFormat, 
                  LPDIRECT3DTEXTURE8* texture, Char* textureFile)
{
    // Get the full filename
    Char path[MAX_PATH];
    findFile(path, textureFile);

    if (FAILED(D3DXCreateTextureFromFileEx(d3dDevice, path, 
               D3DX_DEFAULT, D3DX_DEFAULT, D3DX_DEFAULT, 0, textureFormat, 
               D3DPOOL_MANAGED, D3DX_FILTER_TRIANGLE|D3DX_FILTER_MIRROR, 
               D3DX_FILTER_TRIANGLE|D3DX_FILTER_MIRROR, 0, NULL, NULL, texture)))
    {
        DXCOMMON("(!) createTexture failed for %s", textureFile);
        return dxFailed;
    }
    return dxSuccess;
}



Int setTransparantColor(LPDIRECT3DTEXTURE8 texture, UInt color)
{
    // Get colorkey's red, green, and blue components
    UInt r = ((color&0x00ff0000)>>16);
    UInt g = ((color&0x0000ff00)>>8);
    UInt b = ((color&0x000000ff)>>0);

    // Put the colorkey in the texture's native format
    D3DSURFACE_DESC surface;
    texture->GetLevelDesc(0, &surface);
    if (surface.Format == D3DFMT_A4R4G4B4)
        color = 0xf000 + ((r>>4)<<8) + ((g>>4)<<4) + (b>>4);
    else if (surface.Format == D3DFMT_A1R5G5B5)
        color = 0x8000 + ((r>>3)<<10) + ((g>>3)<<5) + (b>>3);
    else if (surface.Format != D3DFMT_A8R8G8B8)
    {
        DXCOMMON("(!) setTransparantColor : invalid texture format");
        return dxFailed;
    }

    // Lock the texture
    D3DLOCKED_RECT  lockedRect;
    if (FAILED(texture->LockRect(0, &lockedRect, 0, 0)))
    {
        DXCOMMON("(!) setTransparantColor : failed to lock the texture.");
        return dxFailed;
    }

    // Scan through each pixel, looking for the colorkey to replace
    for (UInt y=0; y < surface.Height; y++)
    {
        for (UInt x=0; x < surface.Width; x++)
        {
            if (surface.Format == D3DFMT_A8R8G8B8)
            {
                // Handle 32-bit formats
                if (((DWORD*)lockedRect.pBits)[surface.Width*y+x] == color)
                    ((DWORD*)lockedRect.pBits)[surface.Width*y+x] = 0x00000000;
            }
            else
            {
                // Handle 16-bit formats
                if (((WORD*)lockedRect.pBits)[surface.Width*y+x] == color)
                    ((WORD*)lockedRect.pBits)[surface.Width*y+x] = 0x0000;
            }
        }
    }

    // Unlock the texture and return OK.
    texture->UnlockRect(0);
    return dxSuccess;
}


void convertStringToWide(WCHAR* dest, const Char* source, int stringLen)
{
    if (dest == 0 || source == 0)
        return;

    if (stringLen == -1)
        stringLen = strlen(source)+1;

    MultiByteToWideChar(CP_ACP, 0, source, -1, 
                        dest, stringLen-1);

    dest[stringLen-1] = 0;
}


void convertWideToString(Char* dest, const WCHAR* source, int stringLen)
{
    if (dest == 0 || source == 0)
        return;

    if (stringLen == -1)
        stringLen = wcslen(source)+1;

    WideCharToMultiByte(CP_ACP, 0, source, -1, 
                        dest, stringLen-1, NULL, NULL);

    dest[stringLen-1] = 0;
}


}; // namespace DirectX

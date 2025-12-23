/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <DxCommon/If/Input.h>

//#include <Joystick/resource.h>
#include <Common/If/Algorithm.h>



namespace DirectX
{

// Macro for simplifying keyboard polling
#define KEYPRESSED(buffer, key)   (buffer[key] & 0x80)


InputManager::InputManager( ) :
    m_initialized(false),
    m_directInput(0),
    m_joystick(0),
    m_keyboard(0),
    m_handle(0)
{
    DXCOMMON("(+) InputManager");

}


InputManager::~InputManager( )
{
    finalize( );
    DXCOMMON("(-) InputManager");
}


Int InputManager::initialize(Window::Handle handle)
{
    m_handle = handle;

    if (m_initialized)
    {
        DXCOMMON("InputManager::initialize : already initialized.");
        return dxCallOnlyOnce;
    }
    if (FAILED(DirectInput8Create(GetModuleHandle(0), DIRECTINPUT_VERSION,
                                  IID_IDirectInput8, (VOID**)&m_directInput, 0)))
    {
        DXCOMMON("(!) InputManager::initialize : failed to create DirectInput device");
        return dxFailed;
    }

    if (FAILED(m_directInput->EnumDevices( DI8DEVCLASS_GAMECTRL, InputManager::enumJoysticksCallback,
                                           this, DIEDFL_ATTACHEDONLY)))
    {
        DXCOMMON("(!) InputManager::initialize : failed to enumerate joysticks");
        return dxFailed;
    }

    if (m_joystick == 0)
    {
        DXCOMMON("(!) InputManager::initialize : no joystick found.");
        //return dxJoystickNotFound;
    }
    else
    {
        // Set the data format to "simple joystick" - a predefined data format 
        //
        // A data format specifies which controls on a device we are interested in,
        // and how they should be reported. This tells DInput that we will be
        // passing a DIJOYSTATE2 structure to IDirectInputDevice::GetDeviceState().
    
        if (m_joystick->dataFormat( ) < 0)
        {
            DXCOMMON("(!) InputManager::initialize : failed to set DataFormat for joystick");
            return dxFailed;
        }
    
        // Set the cooperative level to let DInput know how this device should
        // interact with the system and with other DInput applications.
        if (m_joystick->cooperativeLevel(handle /*, DISCL_EXCLUSIVE | DISCL_FOREGROUND */) < 0)
        {
            DXCOMMON("(!) InputManager::initialize : failed to set cooperative level for joystick");
            return dxFailed;
        }
    
        // Enumerate the joystick objects. The callback function enabled user
        // interface elements for objects that are found, and sets the min/max
        // values property for discovered axes.
        if (FAILED(m_joystick->device( )->EnumObjects(InputManager::enumObjectsCallback, (VOID*)this, DIDFT_ALL)))
        {
            DXCOMMON("(!) InputManager::initialize : failed to enumerate objects");
            return dxFailed;
        }
    }

    // Locate Keyboard
    if (FAILED(m_directInput->EnumDevices(DI8DEVCLASS_KEYBOARD, InputManager::enumKeyboardsCallback,
                                          this, DIEDFL_ATTACHEDONLY)))
    {
        DXCOMMON("(!) InputManager::initialize : failed to enumerate keyboards.");
        return dxFailed;
    }

    if (m_keyboard == 0)
    {
        DXCOMMON("(!) InputManager::initialize : no keyboard found.");
        //return dxJoystickNotFound;
    }
    else
    {
        if (m_keyboard->dataFormat( ) < 0)
        {
            DXCOMMON("(!) InputManager::initialize : failed to set DataFormat for keyboard");
            return dxFailed;
        }
    
        // Set the cooperative level to let DInput know how this device should
        // interact with the system and with other DInput applications.
        if (m_keyboard->cooperativeLevel(handle, DISCL_NONEXCLUSIVE | DISCL_FOREGROUND) < 0)
        {
            DXCOMMON("(!) InputManager::initialize : failed to set cooperative level for keyboard");
            return dxFailed;
        }
    }


    m_initialized = true;
    DXCOMMON("InputManager : done initializing.");
    return dxSuccess;

}



void InputManager::finalize( )
{
    if (m_initialized)
    {
        if (m_joystick) 
        {
            delete m_joystick;
            m_joystick = 0;
        }    
        // Release any DirectInput objects.
        SAFE_RELEASE(m_directInput);
        m_initialized = false;
        DXCOMMON("InputManager : finalized");
    }
}


Int InputManager::update( )
{
    Int result = dxSuccess;
    if (m_joystick)
        result |= m_joystick->update( );
    if (m_keyboard)
        result |= m_keyboard->update( );
    return dxSuccess;
}


const Input::State InputManager::state( )
{
    if (m_joystick)
    {
        if (m_keyboard)
        {
            Input::State state1 = m_joystick->state( );
            Input::State state2 = m_keyboard->state( );
            Input::State state3;
            Int x = maximum<Int>(absval<Int>(state1.x), absval<Int>(state2.x));
            if ((state1.x < 0) || (state2.x < 0))
                state3.x = -x;
            else
                state3.x = x;
            Int y = maximum<Int>(absval<Int>(state1.y), absval<Int>(state2.y));
            if ((state1.y < 0) || (state2.y < 0))
                state3.y = -y;
            else
                state3.y = y;
            Int z = maximum<Int>(absval<Int>(state1.z), absval<Int>(state2.z));
            if ((state1.z < 0) || (state2.z < 0))
                state3.z = -z;
            else
                state3.z = z;
            state3.rx = state1.rx;
            state3.ry = state1.ry;
            state3.rz = state1.rz;
            Int slider1 = maximum<Int>(absval<Int>(state1.slider1), absval<Int>(state2.slider1));
            if ((state1.slider1 < 0) || (state2.slider1 < 0))
                state3.slider1 = -slider1;
            else
                state3.slider1 = slider1;
            Int slider2 = maximum<Int>(absval<Int>(state1.slider2), absval<Int>(state2.slider2));
            if ((state1.slider2 < 0) || (state2.slider2 < 0))
                state3.slider2 = -slider2;
            else
                state3.slider2 = slider2;
            state3.b1 = state1.b1 | state2.b1;
            state3.b2 = state1.b2 | state2.b2;
            state3.b3 = state1.b3 | state2.b3;
            state3.b4 = state1.b4 | state2.b4;
            state3.b5 = state1.b5 | state2.b5;
            state3.b6 = state1.b6 | state2.b6;
            state3.b7 = state1.b7 | state2.b7;
            state3.b8 = state1.b8 | state2.b8;
            state3.b9 = state1.b9 | state2.b9;
            state3.b10 = state1.b10 | state2.b10;
            state3.b11 = state1.b11 | state2.b11;
            state3.b12 = state1.b12 | state2.b12;
            state3.b13 = state1.b13 | state2.b13;
            state3.b14 = state1.b14 | state2.b14;
            state3.b15 = state1.b15 | state2.b15;
            state3.b16 = state1.b16 | state2.b16;
            state3.pov1 = state1.pov1 | state2.pov1;
            state3.pov2 = state1.pov2 | state2.pov2;
            state3.pov3 = state1.pov3 | state2.pov3;
            state3.pov4 = state1.pov4 | state2.pov4;
            state3.pov5 = state1.pov5 | state2.pov5;
            state3.pov6 = state1.pov6 | state2.pov6;
            state3.pov7 = state1.pov7 | state2.pov7;
            state3.pov8 = state1.pov8 | state2.pov8;
            memcpy(state3.keys, state2.keys, sizeof(state2.keys));
            return state3;
        }
        else
            return m_joystick->state( );
    }
    else if (m_keyboard)
        return m_keyboard->state( );
    else
    {
        DXCOMMON("(!) InputManager::state : no input device configured!");
        Input::State dummy = {0, 0, 0, false, false, false, false};
        return dummy;
    }
}
        



BOOL CALLBACK InputManager::enumJoysticksCallback( const DIDEVICEINSTANCE* pdidInstance,
                                                 VOID* pContext )
{
    HRESULT hr;

    InputManager*   input = reinterpret_cast<InputManager*>(pContext);
    LPDIRECTINPUTDEVICE8 device;
    // Obtain an interface to the enumerated joystick.
    hr = input->directInput()->CreateDevice( pdidInstance->guidInstance, &device, 0);
    
    // If it failed, then we can't use this joystick. (Maybe the user unplugged
    // it while we were in the middle of enumerating it.)
    if (FAILED(hr)) 
    {
        DXCOMMON("(!) InputManager::EnumDevicesCallback : retrieved an illegal joystick");
        return DIENUM_CONTINUE;
    }

    // Stop enumeration. Note: we're just taking the first joystick we get. You
    // could store all the enumerated joysticks and let the user pick.
    DXCOMMON("InputManager::enumDevicesCallback : retrieved valid joystick.");
    input->m_joystick = new Joystick(device);
    if (pdidInstance->guidFFDriver != GUID_NULL)
    {
        DXCOMMON("InputManager::enumJoysticksCallback : force feedback available");
        input->m_joystick->FFcapable(true);
    }
    return DIENUM_STOP;
}



BOOL CALLBACK InputManager::enumKeyboardsCallback( const DIDEVICEINSTANCE* pdidInstance,
                                                   VOID* pContext )
{
    HRESULT hr;

    InputManager*   input = reinterpret_cast<InputManager*>(pContext);
    LPDIRECTINPUTDEVICE8 device;
    // Obtain an interface to the enumerated joystick.
    hr = input->directInput()->CreateDevice( pdidInstance->guidInstance, &device, 0);

    // If it failed, then we can't use this joystick. (Maybe the user unplugged
    // it while we were in the middle of enumerating it.)
    if (FAILED(hr)) 
    {
        DXCOMMON("(!) InputManager::EnumDevicesCallback : retrieved an illegal keyboard");
        return DIENUM_CONTINUE;
    }

    // Stop enumeration. Note: we're just taking the first joystick we get. You
    // could store all the enumerated joysticks and let the user pick.
    DXCOMMON("InputManager::enumDevicesCallback : retrieved valid keyboard.");
    input->m_keyboard = new Keyboard(device);
    return DIENUM_STOP;
}




//-----------------------------------------------------------------------------
// Name: EnumObjectsCallback()
// Desc: Callback function for enumerating objects (axes, buttons, POVs) on a 
//       joystick. This function enables user interface elements for objects
//       that are found to exist, and scales axes min/max values.
//-----------------------------------------------------------------------------
BOOL CALLBACK InputManager::enumObjectsCallback( const DIDEVICEOBJECTINSTANCE* pdidoi,
                                                 VOID* pContext )
{
    InputManager* input = reinterpret_cast<InputManager*>(pContext);

    
    // For axes that are returned, set the DIPROP_RANGE property for the
    // enumerated axis in order to scale min/max values.
    if( pdidoi->dwType & DIDFT_AXIS )
    {
        if (input->joystick( )->setRange(pdidoi->dwType, -100, 100) < 0)
        {
            DXCOMMON("(!) InputManager::enumObjectsCallback : error setting range for joystick axis.");
            return DIENUM_STOP;
        }
    }

    
    // Set the supported axis and buttons on the joystick
    if (pdidoi->guidType == GUID_XAxis)
        input->joystick( )->addAvailable(Input::Xaxis);
    else if (pdidoi->guidType == GUID_YAxis)
        input->joystick( )->addAvailable(Input::Yaxis);
    else if (pdidoi->guidType == GUID_ZAxis)
        input->joystick( )->addAvailable(Input::Zaxis);
    else if (pdidoi->guidType == GUID_RxAxis)
        input->joystick( )->addAvailable(Input::RXaxis);
    else if (pdidoi->guidType == GUID_RyAxis)
        input->joystick( )->addAvailable(Input::RYaxis);
    else if (pdidoi->guidType == GUID_RzAxis)
        input->joystick( )->addAvailable(Input::RZaxis);
    else if (pdidoi->guidType == GUID_Slider)
        input->joystick( )->addAvailable(Input::slider);
    else if (pdidoi->guidType == GUID_POV)
        input->joystick( )->addAvailable(Input::pov);
    // No data about the number of buttons, set them all to available
    input->joystick( )->addAvailable(Input::button01 | Input::button02 |
                                     Input::button03 | Input::button04 |
                                     Input::button05 | Input::button06 |
                                     Input::button07 | Input::button08 |
                                     Input::button09 | Input::button10 |
                                     Input::button11 | Input::button12 |
                                     Input::button13 | Input::button14 |
                                     Input::button15 | Input::button16);
    return DIENUM_CONTINUE;
}



Keyboard::Keyboard(LPDIRECTINPUTDEVICE8 device) :
    m_device(device),
    m_available(Input::none)
{
    DXCOMMON("(+) Keyboard");
    m_state.x = 0;
    m_state.y = 0;
    m_state.z = 0;
    m_state.rx = 0;
    m_state.ry = 0;
    m_state.rz = 0;
    m_state.slider1 = 0;
    m_state.slider2 = 0;
    m_state.b1 = false;
    m_state.b2 = false;
    m_state.b3 = false;
    m_state.b4 = false;
    m_state.b5 = false;
    m_state.b6 = false;
    m_state.b7 = false;
    m_state.b8 = false;
    m_state.b9 = false;
    m_state.b10 = false;
    m_state.b11 = false;
    m_state.b12 = false;
    m_state.b13 = false;
    m_state.b14 = false;
    m_state.b15 = false;
    m_state.b16 = false;
    m_state.pov1 = false;
    m_state.pov2 = false;
    m_state.pov3 = false;
    m_state.pov4 = false;
    m_state.pov5 = false;
    m_state.pov6 = false;
    m_state.pov7 = false;
    m_state.pov8 = false;
}


Keyboard::~Keyboard( )
{
    DXCOMMON("(-) Keyboard");
    if (m_device) 
        m_device->Unacquire();
}


Int Keyboard::dataFormat( )
{
    if (FAILED(m_device->SetDataFormat(&c_dfDIKeyboard)))
    {
        DXCOMMON("(!) Keyboard::dataFormat : failed to set DataFormat for keyboard");
        return dxFailed;
    }
    return dxSuccess;
}


Int Keyboard::cooperativeLevel(::Window::Handle handle, UInt flags)
{
    if (FAILED(m_device->SetCooperativeLevel(handle, flags)))
    {
        DXCOMMON("(!) Keyboard::cooperativeLevel : failed to set cooperative level for keyboard");
        return dxFailed;
    }
    return dxSuccess;
}


Int Keyboard::update( )
{
    HRESULT     hr;

    if (m_device == 0)
        return dxSuccess;

    // Poll the device to read the current state
    hr = m_device->Poll(); 
    if (FAILED(hr))  
    {
        hr = m_device->Acquire();
        while (hr == DIERR_INPUTLOST) 
            hr = m_device->Acquire();
//        return dxSuccess; 
    }

    // Get the input's device state
    if (FAILED(hr = m_device->GetDeviceState(sizeof(m_keys), &m_keys)))
        return dxFailed;
    
    // Update state according polled data
    // if (KEYPRESSED(m_keys, DIK_UP))
        // m_state.y   = 100;
    // else if (KEYPRESSED(m_keys, DIK_DOWN))
        // m_state.y   = -100;
    // else 
        m_state.y   = 0;
    // if (KEYPRESSED(m_keys, DIK_LEFT))
        // m_state.x   = -100;
    // else if (KEYPRESSED(m_keys, DIK_RIGHT))
        // m_state.x   = 100;
    // else
        m_state.x   = 0;
    m_state.z       = 0;
    m_state.rx      = 0;
    m_state.ry      = 0;
    m_state.rz      = 0;
    m_state.slider1 = 0;
    m_state.slider2 = 0;
    m_state.b1      = (KEYPRESSED(m_keys, DIK_RETURN) != 0);
    m_state.b2      = (KEYPRESSED(m_keys, DIK_RSHIFT) != 0);
    m_state.b3      = (KEYPRESSED(m_keys, DIK_RALT) != 0);
    m_state.b4      = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b5      = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b6      = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b7      = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b8      = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b9      = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b10     = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b11     = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b12     = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b13     = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b14     = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b15     = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.b16     = (KEYPRESSED(m_keys, DIK_SPACE) != 0);
    m_state.pov1    = (KEYPRESSED(m_keys, DIK_UP) != 0);
    m_state.pov2    = (KEYPRESSED(m_keys, DIK_RIGHT) != 0);
    m_state.pov3    = (KEYPRESSED(m_keys, DIK_DOWN) != 0);
    m_state.pov4    = (KEYPRESSED(m_keys, DIK_LEFT) != 0);
    m_state.pov5    = (KEYPRESSED(m_keys, DIK_UP) != 0);
    m_state.pov6    = (KEYPRESSED(m_keys, DIK_RIGHT) != 0);
    m_state.pov7    = (KEYPRESSED(m_keys, DIK_DOWN) != 0);
    m_state.pov8    = (KEYPRESSED(m_keys, DIK_LEFT) != 0);
    memcpy(m_state.keys, m_keys, sizeof(m_keys));
    return dxSuccess;
}


Joystick::Joystick(LPDIRECTINPUTDEVICE8 device) :
    m_device(device),
    m_available(Input::none),
    m_forceFeedbackCapable(false)
{
    DXCOMMON("(+) Joystick");
    m_state.x = 0;
    m_state.y = 0;
    m_state.z = 0;
    m_state.rx = 0;
    m_state.ry = 0;
    m_state.rz = 0;
    m_state.slider1 = 0;
    m_state.slider2 = 0;
    m_state.b1 = false;
    m_state.b2 = false;
    m_state.b3 = false;
    m_state.b4 = false;
    m_state.b5 = false;
    m_state.b6 = false;
    m_state.b7 = false;
    m_state.b8 = false;
    m_state.b9 = false;
    m_state.b10 = false;
    m_state.b11 = false;
    m_state.b12 = false;
    m_state.b13 = false;
    m_state.b14 = false;
    m_state.b15 = false;
    m_state.b16 = false;
    m_state.pov1 = false;
    m_state.pov2 = false;
    m_state.pov3 = false;
    m_state.pov4 = false;
    m_state.pov5 = false;
    m_state.pov6 = false;
    m_state.pov7 = false;
    m_state.pov8 = false;
}



Joystick::~Joystick( )
{
    DXCOMMON("(-) Joystick");
    if (m_device) 
        m_device->Unacquire();
}


Int Joystick::update( )
{
    HRESULT     hr;
    DIJOYSTATE2 js;           // DInput joystick state 

    if (m_device == 0)
        return dxSuccess;

    // Poll the device to read the current state
    hr = m_device->Poll(); 
    if (FAILED(hr))  
    {
        hr = m_device->Acquire();
        while (hr == DIERR_INPUTLOST) 
            hr = m_device->Acquire();
        return dxSuccess; 
    }

    // Get the input's device state
    if (FAILED(hr = m_device->GetDeviceState(sizeof(DIJOYSTATE2), &js)))
        return dxFailed;
    
    // Update state according polled data
    if (abs(js.lX) < 9)
        m_state.x = 0;
    else
        m_state.x  = (Int) js.lX;
    if (abs(js.lY) < 9)
        m_state.y = 0;
    else
        m_state.y  = (Int) -js.lY;
    if (abs(js.lZ) < 9)
        m_state.z = 0;
    else
        m_state.z  = (Int) js.lZ;
    if (abs(js.lRx) < 9)
        m_state.rx = 0;
    else
        m_state.rx  = (Int) js.lRx;
    if (abs(js.lRy) < 9)
        m_state.ry = 0;
    else
        m_state.ry  = (Int) js.lRy;
    if (abs(js.lRz) < 9)
        m_state.rz = 0;
    else
        m_state.rz  = (Int) js.lRz;
    if (abs(js.rglSlider[0]) < 9)
        m_state.slider1 = 0;
    else
        m_state.slider1 = (Int) js.rglSlider[0];
    if (abs(js.rglSlider[1]) < 9)
        m_state.slider2 = 0;
    else
        m_state.slider2 = (Int) js.rglSlider[1];
    m_state.b1 = (js.rgbButtons[0] & 0x80) != 0;
    m_state.b2 = (js.rgbButtons[1] & 0x80) != 0;
    m_state.b3 = (js.rgbButtons[2] & 0x80) != 0;
    m_state.b4 = (js.rgbButtons[3] & 0x80) != 0;
    m_state.b5 = (js.rgbButtons[4] & 0x80) != 0;
    m_state.b6 = (js.rgbButtons[5] & 0x80) != 0;
    m_state.b7 = (js.rgbButtons[6] & 0x80) != 0;
    m_state.b8 = (js.rgbButtons[7] & 0x80) != 0;
    m_state.b9 = (js.rgbButtons[8] & 0x80) != 0;
    m_state.b10 = (js.rgbButtons[9] & 0x80) != 0;
    m_state.b11 = (js.rgbButtons[10] & 0x80) != 0;
    m_state.b12 = (js.rgbButtons[11] & 0x80) != 0;
    m_state.b13 = (js.rgbButtons[12] & 0x80) != 0;
    m_state.b14 = (js.rgbButtons[13] & 0x80) != 0;
    m_state.b15 = (js.rgbButtons[14] & 0x80) != 0;
    m_state.b16 = (js.rgbButtons[15] & 0x80) != 0;
    if ((js.rgdwPOV[0] < 0) || (js.rgdwPOV[0] > 36000))
    {
        m_state.pov1 = false;
        m_state.pov2 = false;
        m_state.pov3 = false;
        m_state.pov4 = false;
    }
    else
    {
        m_state.pov1 = (js.rgdwPOV[0] > 31500) || (js.rgdwPOV[0] < 4500);
        m_state.pov2 = (js.rgdwPOV[0] > 4500) && (js.rgdwPOV[0] < 13500);
        m_state.pov3 = (js.rgdwPOV[0] > 13500) && (js.rgdwPOV[0] < 22500);
        m_state.pov4 = (js.rgdwPOV[0] > 22500) && (js.rgdwPOV[0] < 31500);
    }
    if ((js.rgdwPOV[1] < 0) || (js.rgdwPOV[1] > 36000))
    {
        m_state.pov5 = false;
        m_state.pov6 = false;
        m_state.pov7 = false;
        m_state.pov8 = false;
    }
    else
    {
        m_state.pov5 = (js.rgdwPOV[1] > 31500) || (js.rgdwPOV[1] < 4500);
        m_state.pov6 = (js.rgdwPOV[1] > 4500) && (js.rgdwPOV[1] < 13500);
        m_state.pov7 = (js.rgdwPOV[1] > 13500) && (js.rgdwPOV[1] < 22500);
        m_state.pov8 = (js.rgdwPOV[1] > 22500) && (js.rgdwPOV[1] < 31500);
    }
    return dxSuccess;
}



Int Joystick::setRange(UInt axis, Int min, Int max)
{
    DXCOMMON("Joystick::setRange : setting range for axis %d.", axis);
    DIPROPRANGE diprg; 
    diprg.diph.dwSize       = sizeof(DIPROPRANGE); 
    diprg.diph.dwHeaderSize = sizeof(DIPROPHEADER); 
    diprg.diph.dwHow        = DIPH_BYID; 
    diprg.diph.dwObj        = axis;
    diprg.lMin              = min; 
    diprg.lMax              = max; 
    
    // Set the range for the axis
    if(FAILED(m_device->SetProperty(DIPROP_RANGE, &diprg.diph))) 
    {
        DXCOMMON("(!) Joystick::setRange : failed to set range for axis %d.", axis);
        return dxFailed;
    }

    return dxSuccess;
}



Int Joystick::dataFormat( )
{
    if (FAILED(m_device->SetDataFormat(&c_dfDIJoystick2)))
    {
        DXCOMMON("(!) Joystick::dataFormat : failed to set DataFormat for joystick");
        return dxFailed;
    }
    return dxSuccess;
}



Int Joystick::cooperativeLevel(::Window::Handle handle /*, UInt flags */)
{
    if (FAILED(m_device->SetCooperativeLevel(handle, DISCL_EXCLUSIVE | DISCL_BACKGROUND)))
    {
        DXCOMMON("(!) Joystick::cooperativeLevel : failed to set cooperative level for joystick");
        return dxFailed;
    }
    return dxSuccess;
}


void    
Joystick::autocenter(Boolean b)
{
    if (m_forceFeedbackCapable)
    {
        if (b)
        {
            DXCOMMON("Joystick::autocenter : true");
            DIPROPDWORD dipdw;
            dipdw.diph.dwSize       = sizeof(DIPROPDWORD);
            dipdw.diph.dwHeaderSize = sizeof(DIPROPHEADER);
            dipdw.diph.dwObj        = 0;
            dipdw.diph.dwHow        = DIPH_DEVICE;
            dipdw.dwData            = DIPROPAUTOCENTER_ON;

            HRESULT res;
            if (FAILED(res = m_device->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph)))
            {
                DXCOMMON("(!) Joystick::autocenter : error 0x%x", res);
            }
        }
        else
        {
            DXCOMMON("Joystick::autocenter : false");
            DIPROPDWORD dipdw;
            dipdw.diph.dwSize       = sizeof(DIPROPDWORD);
            dipdw.diph.dwHeaderSize = sizeof(DIPROPHEADER);
            dipdw.diph.dwObj        = 0;
            dipdw.diph.dwHow        = DIPH_DEVICE;
            dipdw.dwData            = DIPROPAUTOCENTER_OFF;

            HRESULT res;
            if (FAILED(res = m_device->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph)))
            {
                DXCOMMON("(!) Joystick::autocenter : error 0x%x", res);
            }
        }
    }
}



ForceFeedbackEffect::ForceFeedbackEffect(Joystick* joystick) :
    m_joystick(joystick)
{
    DXCOMMON("(+) ForceFeedbackEffect");

}


ForceFeedbackEffect::ForceFeedbackEffect(Joystick* joystick, char* filename) :
    m_joystick(joystick)
{
    DXCOMMON("(+) ForceFeedbackEffect : file = '%s'", filename);
    HRESULT res;
    if (FAILED(res = m_joystick->device( )->EnumEffectsInFile(filename, 
                ForceFeedbackEffect::EnumAndCreateEffectsCallback,
                this, DIFEF_MODIFYIFNEEDED)))
    {
        DXCOMMON("(!) ForceFeedbackEffect : file = '%s', error 0x%x reading file", res);
    }
}


ForceFeedbackEffect::~ForceFeedbackEffect( )
{
    DXCOMMON("(-) ForceFeedbackEffect");
    EffectVector::iterator effectIt;
    for (effectIt = m_effects.begin( ); effectIt != m_effects.end( ); effectIt++)
    {
        if (*effectIt != NULL)
        {
            SAFE_RELEASE(*effectIt);
        }
    }
}


void
ForceFeedbackEffect::play( )
{
    EffectVector::iterator effectIt;
    for (effectIt = m_effects.begin( ); effectIt != m_effects.end( ); effectIt++)
    {
        if (*effectIt != NULL)
        {
            HRESULT res;
            if (FAILED(res = (*effectIt)->Start(0, 0)))
            {
                DXCOMMON("(!) ForceFeedbackEffect::play : error 0x%x", res);
            }
        }
    }
}


void
ForceFeedbackEffect::stop( )
{
    EffectVector::iterator effectIt;
    for (effectIt = m_effects.begin( ); effectIt != m_effects.end( ); effectIt++)
    {
        if (*effectIt != NULL)
        {
            HRESULT res;
            if (FAILED(res = (*effectIt)->Stop( )))
            {
                DXCOMMON("(!) ForceFeedbackEffect::stop : error 0x%x", res);
            }
        }
    }
}


void 
ForceFeedbackEffect::gain(UInt gain)
{
    EffectVector::iterator effectIt;
    for (effectIt = m_effects.begin( ); effectIt != m_effects.end( ); effectIt++)
    {
        if (*effectIt != NULL)
        {
            if (gain > 10000)
                gain = 10000;
            HRESULT res;
            DIEFFECT effect;
            ZeroMemory(&effect, sizeof(effect));
            effect.dwSize = sizeof(DIEFFECT);
            effect.dwGain = gain;
            
            if (FAILED(res = (*effectIt)->SetParameters(&effect, DIEP_GAIN)))
            {
                DXCOMMON("(!) ForceFeedbackEffect::gain : error 0x%x", res);
            }
        }
    }
}


BOOL CALLBACK 
ForceFeedbackEffect::EnumAndCreateEffectsCallback(LPCDIFILEEFFECT effect, VOID* classRef)
{
    ForceFeedbackEffect* ffEffect = (ForceFeedbackEffect*) classRef;
    ffEffect->addFileEffect(effect);
    return DIENUM_CONTINUE;
}

void 
ForceFeedbackEffect::addFileEffect(LPCDIFILEEFFECT fileEffect)
{
    HRESULT res;
    LPDIRECTINPUTEFFECT effect = NULL;

    // Create the file effect
    if (FAILED(res = m_joystick->device( )->CreateEffect(fileEffect->GuidEffect, 
                                                         fileEffect->lpDiEffect, 
                                                         &effect, NULL)))
    {
        DXCOMMON("(!) ForceFeedbackEffect::addFileEffect : error 0x%x", res);
        return;
    }

    m_effects.insert(m_effects.end(), effect);
}


} // namespace DirectX

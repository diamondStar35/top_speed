/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_CARDEFS_H__
#define __RACING_CARDEFS_H__

#define NVEHICLES 12

// NEVER INCLUDE THIS FILE IN A HEADER!!
/*
    struct Parameters
    {
        // parameters 
        Int                     engineSound;
        Int                     startSound;
        Int                     hornSound
        Int                     throttleSound
        Int                     crashSound
        Int                     monoCrashSound
        Int                     brakeSound
        Int                     backfireSound
        Int                     hasWipers;
        Int                     acceleration;
        Int                     deceleration;
        Int                     topspeed;
        Int                     idlefreq;
        Int                     topfreq;
        Int                     shiftfreq;
        Int                     gears;
        Int                     steering;
        Int                     steeringFactor;
    };
*/

Car::Parameters _vhc1 = 
{
    IDR_VEHICLE1E,
    IDR_VEHICLE1S,
    IDR_VEHICLE1H,
    IDR_VEHICLE1T,
    IDR_VEHICLE1C,
    IDR_VEHICLE1CM,
    IDR_VEHICLE1B,
    0,
    1,
    11,
    40,
    17500,
    22050,
    55000,
    26000,
    5,
    160,
    60
};

Car::Parameters _vhc2 = 
{
    IDR_VEHICLE2E,
    IDR_VEHICLE2S,
    IDR_VEHICLE2H,
    IDR_VEHICLE2T,
    IDR_VEHICLE1C,
    IDR_VEHICLE1CM,
    IDR_VEHICLE1B,
    0,
    1,
    13,
    35,
    18500,
    22050,
    60000,
    35000,
    5,
    150,
    55
};

Car::Parameters _vhc3 = 
{
    IDR_VEHICLE3E,
    IDR_VEHICLE1S,
    IDR_VEHICLE3H,
    0,
    IDR_VEHICLE3C,
    IDR_VEHICLE3CM,
    IDR_VEHICLE3B,
    0,
    1,
    10,
    35,
    15100,
    6000,
    25000,
    19000,
    4,
    150,
    72
};


Car::Parameters _vhc4 = 
{
    IDR_VEHICLE4E,
    IDR_VEHICLE1S,
    IDR_VEHICLE4H,
    0,
    IDR_VEHICLE3C,
    IDR_VEHICLE3CM,
    IDR_VEHICLE3B,
    0,
    1,
    12,
    40,
    17200,
    6000,
    27000,
    20000,
    6,
    140,
    56
};


Car::Parameters _vhc5 = 
{
    IDR_VEHICLE5E,
    IDR_VEHICLE1S,
    IDR_VEHICLE5H,
    0,
    IDR_VEHICLE1C,
    IDR_VEHICLE1CM,
    IDR_VEHICLE1B,
    0,
    1,
    12,
    60,
    24000,
    6000,
    33000,
    27500,
    4,
    230,
    80
};

Car::Parameters _vhc6 =
{
    IDR_VEHICLE6E,
    IDR_VEHICLE1S,
    IDR_VEHICLE6H,
    0,
    IDR_VEHICLE1C,
    IDR_VEHICLE1CM,
    IDR_VEHICLE6B,
    0,
    1,
    9,
    90,
    26000,
    7025,
    40000,
    32500,
    6,
    220,
    95
};

Car::Parameters _vhc7 = 
{
    IDR_VEHICLE7E,
    IDR_VEHICLE1S,
    IDR_VEHICLE3H,
    0,
    IDR_VEHICLE1C,
    IDR_VEHICLE1CM,
    IDR_VEHICLE3B,
    0,
    1,
    13,
    70,
    21000,
    6000,
    26000,
    21000,
    5,
    210,
    65
};

Car::Parameters _vhc8 = 
{
    IDR_VEHICLE8E,
    IDR_VEHICLE1S,
    IDR_VEHICLE6H,
    0,
    IDR_VEHICLE1C,
    IDR_VEHICLE1CM,
    IDR_VEHICLE6B,
    0,
    1,
    11,
    55,
    23000,
    10000,
    45000,
    34000,
    5,
    200,
    70
};

Car::Parameters _vhc9 =
{
    IDR_VEHICLE9E,
    IDR_VEHICLE9S,
    IDR_VEHICLE9H,
    IDR_VEHICLE9T,
    IDR_VEHICLE9C,
    IDR_VEHICLE9CM,
    IDR_VEHICLE9B,
    IDR_VEHICLE9F,
    1,
    8,
    25,
    18000,
    22050,
    30550,
    22550,
    5,
    150,
    85
};

Car::Parameters _vhc10 = 
{
    IDR_VEHICLE10E,
    IDR_VEHICLE10S,
    IDR_VEHICLE10H,
    0,
    IDR_VEHICLE10C,
    IDR_VEHICLE10CM,
    IDR_VEHICLE1B,
    0,
    0,
    15,
    45,
    20000,
    22050,
    60000,
    35000,
    5,
    140,
    50
};

Car::Parameters _vhc11 =
{
    IDR_VEHICLE11E,
    IDR_VEHICLE11S,
    IDR_VEHICLE10H,
    0,
    IDR_VEHICLE10C,
    IDR_VEHICLE10CM,
    IDR_VEHICLE1B,
    0,
    0,
    17,
    40,
    22000,
    22050,
    60000,
    35000,
    5,
    130,
    50
};

Car::Parameters _vhc12 =
{
    IDR_VEHICLE12E,
    IDR_VEHICLE12S,
    IDR_VEHICLE12H,
    IDR_VEHICLE12T,
    IDR_VEHICLE10C,
    IDR_VEHICLE10CM,
    IDR_VEHICLE1B,
    IDR_VEHICLE12F,
    0,
    13,
    45,
    24000,
    22050,
    27550,
    23550,
    5,
    150,
    66
};

Car::Parameters vehicles[NVEHICLES] = {_vhc1, _vhc2, _vhc3, _vhc4, _vhc5, _vhc6, _vhc7, _vhc8, _vhc9, _vhc10, _vhc11, _vhc12};


#endif /* __RACING_CARDEFS_H__ */

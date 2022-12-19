using System;

namespace Hotd3Arcade_Launcher
{
    public class Configurator
    {

        #region Constants

        private const String CONF_FILENAME = "Hod3Arcade.ini";

        private const string OPTION_FILEPATH = "FilePath";
        
        private const string OPTION_VOLUME_BGM = "VolumeBGM";
        private const string OPTION_VOLUME_SFX = "VolumeSFX";
        private const string OPTION_VOLUME_VCE = "VolumeVCE";        
        private const string OPTION_DIFFICULTY = "Difficulty";
        private const string OPTION_INITIAL_LIFE = "InitialLife";
        private const string OPTION_MAX_LIFE = "MaxLife";
        private const string OPTION_BLOOD_COLOR = "BloodColor";
        private const string OPTION_VIOLENCE = "Violence";
        private const string OPTION_ADVERTISE = "AdvertiseSound";
        private const string OPTION_CREDITS_TO_START = "CreditsToStart";
        private const string OPTION_CREDITS_TO_CONTINUE = "CreditsToContinue";
        private const string OPTION_FREEPLAY = "Freeplay";
        private const string OPTION_CROSSHAIR_DESIGN = "CrosshairDesign";
        private const string OPTION_CROSSHAIR_SPEED = "CrosshairSpeed";

        private const string OPTION_ALIASING = "AntiAliasing";
        private const string OPTION_COLORS = "Colors";
        private const string OPTION_TEXTURES = "Textures";
        private const string OPTION_RESOLUTION = "Resolution";
        private const string OPTION_FRAMERATE = "Framerate";
        private const string OPTION_FULLSCREEN = "Fullscreen";
        private const string OPTION_P1CONTROLS = "P1Controls";
        private const string OPTION_P2CONTROLS = "P2Controls";
        private const string OPTION_MOUSE_SHOOT = "MouseShoot";
        private const string OPTION_MOUSE_RELOAD = "MouseReload";
        private const string OPTION_KEYBOARD1P_CENTER = "Keyboard1pCenter";
        private const string OPTION_KEYBOARD1P_UP = "Keyboard1pUp";
        private const string OPTION_KEYBOARD1P_DOWN = "Keyboard1pDown";
        private const string OPTION_KEYBOARD1P_LEFT = "Keyboard1pLeft";
        private const string OPTION_KEYBOARD1P_RIGHT = "Keyboard1pRight";
        private const string OPTION_KEYBOARD1P_START = "Keyboard1pStart";
        private const string OPTION_KEYBOARD1P_SHOOT = "Keyboard1pShoot";
        private const string OPTION_KEYBOARD1P_RELOAD = "Keyboard1pReload";
        private const string OPTION_KEYBOARD2P_CENTER = "Keyboard2pCenter";
        private const string OPTION_KEYBOARD2P_UP = "Keyboard2pUp";
        private const string OPTION_KEYBOARD2P_DOWN = "Keyboard2Down";
        private const string OPTION_KEYBOARD2P_LEFT = "Keyboard2pLeft";
        private const string OPTION_KEYBOARD2P_RIGHT = "Keyboard2pRight";
        private const string OPTION_KEYBOARD2P_START = "Keyboard2pStart";
        private const string OPTION_KEYBOARD2P_SHOOT = "Keyboard2pShoot";
        private const string OPTION_KEYBOARD2P_RELOAD = "Keyboard2pReload";
        private const string OPTION_GAMEPAD1P_SHOOT = "Gamepad1pShoot";
        private const string OPTION_GAMEPAD1P_RELOAD = "Gamepad1pReload";
        private const string OPTION_GAMEPAD1P_CENTER = "Gamepad1pCenter";
        private const string OPTION_GAMEPAD1P_START = "Gamepad1pStart";
        private const string OPTION_GAMEPAD2P_SHOOT = "Gamepad2pShoot";
        private const string OPTION_GAMEPAD2P_RELOAD = "Gamepad2pReload";
        private const string OPTION_GAMEPAD2P_CENTER = "Gamepad2pCenter";
        private const string OPTION_GAMEPAD2P_START = "Gamepad2pStart";
        private const string OPTION_LANGUAGE = "Language";

        private const string OPTION_HIDE_CROSSHAIR = "HideCrosshairs";
        private const string OPTION_HIDE_CURSOR = "HideCursor";
        private const string OPTION_DISABLE_PAUSE = "DisableInGamePause";

        #endregion

        #region ConfigurationItems

        //Byte array for replacing Register values
        private UInt32[] _RegValues = new UInt32[] { 
            0x01,   //CtrlType1P            0=Keyb, 1=Mouse, 2=Gamepad
            0x00,   //CtrlType2p            0=Keyb, 1=Mouse, 2=Gamepad
            0x00,   //Mouseshoot            0=Lbtn, 1=Rbtn 
            0x01,   //MouseReload           0=Lbtn, 1=Rbtn
            //+0x0C offset
            0x18,   //KeyBoard1pShoot
            0x25,   //KeyBoard2pShoot
            0x19,   //KeyBoard1pReload
            0x26,   //KeyBoard2pReload
            0xC8,   //KeyBoard1pUp
            0x48,   //KeyBoard2pUp
            0x50,   //KeyBoard1pDown
            0xD0,   //KeyBoard2pDown
            0xCB,   //KeyBoard1pLeft
            0x4B,   //KeyBoard2pLeft
            0xCD,   //KeyBoard1pRight
            0x4D,   //KeyBoard2pRight
            0x02,   //KeyBoard1pStart
            0x03,   //KeyBoard2pStart
            0x11,   //KeyBoard1pCenter
            0x15,   //KeyBoard2pCenter
            0x00,   //GamePad1p1
            0x00,   //GamePad2p1
            0x01,   //GamePad1p2
            0x01,   //GamePad2p2
            0x02,   //GamePad1p3
            0x02,   //GamePad2p3
            0x03,   //GamePad1p4
            0x03,   //GamePad2p4
            0x00,   //GamePad1Vb
            0x00,   //GamePad2Vb
            //+0x0C offset
            0x03,   //Graphicalresolution       //0=640x480, 1=800x600, 2=1024x768, 3=1280x960, 4=1280x1024
            0x01,   //Fullscreen                //0=OFF, 1=ON
            0x00,   //Framerate                 //0=60, 1=30, 2=20, 3=15, 4=10
            0x01,   //Antialiasing              //1=OFF, 0=ON
            0x02,   //Texture                   //0=Low, 1=Medium, 2=High
            0x01,   //ColorMode                 //1=32Bits, 2=16Bits
            0x00,   //Model
            0x00    //Language                  //0=English; 1=French, 2=Italian, 3=Spanish
        };

        //These one are store in HOD3.DAT file save
        private byte _VolumeBGM = 0xFF;         //[00-FF] step 0x08
        private byte _VolumeSFX = 0xFF;         //[00-FF] step 0x08
        private byte _VolumeVCE = 0xFF;         //[00-FF] step 0x08
        private byte _GameDifficulty = 2;       //[0-4] VERYEASY-MEDIUM EASY-NORMAL-MEDIUM HARD-VERY HARD
        private byte _InitialLife = 3;          //[0-4]
        private byte _Freeplay = 1;             //put FF in credits to start
        private byte _Violence = 0;             //[0-1-2]GRATUITOUS, MEDIUM or MILD
        private byte _BloodColor = 1;           //RED-GREEN [00 00 / 01 01]
        private byte _CrosshairDesign = 2;      //[0-3]
        private byte _CrosshairSpeed = 0xFF;    //[00-FF]

        //These ones are custom        
        private UInt32 _HideCursor = 1;
        private UInt32 _HideCrosshairs = 1;
        private UInt32 _DisablePause = 1;
        private UInt32 _CreditsToStart = 1;
        private UInt32 _CreditsToContinue = 1;
        private UInt32 _MaxLife = 5;
        private String _GameFilePath = String.Empty;
            
        #endregion

        #region Accessors

        public byte VolumeBGM
        { get { return _VolumeBGM; } }

        public byte VolumeSFX
        { get { return _VolumeSFX; } }

        public byte VolumeVCE
        { get { return _VolumeVCE; } }

        public byte GameDifficulty
        { get { return _GameDifficulty; } }

        public byte InitialLife
        { get { return (byte)(_InitialLife - 1) ; } }

        public byte Freeplay
        { get { return _Freeplay; } }

        public byte Violence
        { get { return _Violence; } }

        public byte BloodColor
        { get { return _BloodColor; } }

        public byte CrosshairDesign
        { get { return _CrosshairDesign; } }

        public byte CrosshairSpeed
        { get { return _CrosshairSpeed; } }

        public UInt32 HideCursor
        { get { return _HideCursor; } }

        public UInt32 HideCrosshairs
        { get { return _HideCrosshairs; } }

        public UInt32 DisablePause
        { get { return _DisablePause; } }

        public UInt32 CreditsToStart
        { get { return _CreditsToStart; } }

        public UInt32 CreditsToContinue
        { get { return _CreditsToContinue; } }

        public UInt32 MaxLife
        { get { return _MaxLife; } }

        public String GameFilePath
        { get { return _GameFilePath; } }

        public UInt32[] RegValues
        { get { return _RegValues; } }

        public UInt32 Language
        { get { return _RegValues[37]; } }

        #endregion

        public Configurator()
        {}

        private void SetOptionInArray(UInt32 iValue, int Index)
        {
            _RegValues[Index] = iValue;
        }

        public bool ReadConf()
        {
            try
            {
                String[] lines = System.IO.File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + CONF_FILENAME);
                foreach (String line in lines)
                {
                    if (!line.StartsWith("[") && !line.StartsWith(";"))
                    {

                        if (line.StartsWith(OPTION_FILEPATH + ":"))
                        {
                            _GameFilePath = line.Substring(OPTION_FILEPATH.Length + 1).Replace("\"", "");
                        }
                        else
                        {
                            String[] buffer = line.Split(':');
                            String Key = buffer[0];
                            String Value = buffer[1];
                            UInt32 i_Value = 0;

                            if (UInt32.TryParse(Value, out i_Value))
                            {
                                switch (Key)
                                {
                                    case OPTION_P1CONTROLS:
                                        {
                                            SetOptionInArray(i_Value, 0);
                                        } break;
                                    case OPTION_P2CONTROLS:
                                        {
                                            SetOptionInArray(i_Value, 1);
                                        } break;
                                    case OPTION_MOUSE_SHOOT:
                                        {
                                            SetOptionInArray(i_Value, 2);
                                        } break;
                                    case OPTION_MOUSE_RELOAD:
                                        {
                                            SetOptionInArray(i_Value, 3);
                                        } break;
                                    case OPTION_KEYBOARD1P_SHOOT:
                                        {
                                            SetOptionInArray(i_Value, 4);
                                        } break;
                                    case OPTION_KEYBOARD2P_SHOOT:
                                        {
                                            SetOptionInArray(i_Value, 5);
                                        } break;
                                    case OPTION_KEYBOARD1P_RELOAD:
                                        {
                                            SetOptionInArray(i_Value, 6);
                                        } break;
                                    case OPTION_KEYBOARD2P_RELOAD:
                                        {
                                            SetOptionInArray(i_Value, 7);
                                        } break;
                                    case OPTION_KEYBOARD1P_UP:
                                        {
                                            SetOptionInArray(i_Value, 8);
                                        } break;
                                    case OPTION_KEYBOARD2P_UP:
                                        {
                                            SetOptionInArray(i_Value, 9);
                                        } break;
                                    case OPTION_KEYBOARD1P_DOWN:
                                        {
                                            SetOptionInArray(i_Value, 10);
                                        } break;
                                    case OPTION_KEYBOARD2P_DOWN:
                                        {
                                            SetOptionInArray(i_Value, 11);
                                        } break;
                                    case OPTION_KEYBOARD1P_LEFT:
                                        {
                                            SetOptionInArray(i_Value, 12);
                                        } break;
                                    case OPTION_KEYBOARD2P_LEFT:
                                        {
                                            SetOptionInArray(i_Value, 13);
                                        } break;
                                    case OPTION_KEYBOARD1P_RIGHT:
                                        {
                                            SetOptionInArray(i_Value, 14);
                                        } break;
                                    case OPTION_KEYBOARD2P_RIGHT:
                                        {
                                            SetOptionInArray(i_Value, 15);
                                        } break;
                                    case OPTION_KEYBOARD1P_START:
                                        {
                                            SetOptionInArray(i_Value, 16);
                                        } break;
                                    case OPTION_KEYBOARD2P_START:
                                        {
                                            SetOptionInArray(i_Value, 17);
                                        } break;
                                    case OPTION_KEYBOARD1P_CENTER:
                                        {
                                            SetOptionInArray(i_Value, 18);
                                        } break;
                                    case OPTION_KEYBOARD2P_CENTER:
                                        {
                                            SetOptionInArray(i_Value, 19);
                                        } break;

                                    //Gamepad
                                    case OPTION_GAMEPAD1P_SHOOT:
                                        {
                                            SetOptionInArray(i_Value, 20);
                                        } break;
                                    case OPTION_GAMEPAD2P_SHOOT:
                                        {
                                            SetOptionInArray(i_Value, 21);
                                        } break;
                                    case OPTION_GAMEPAD1P_RELOAD:
                                        {
                                            SetOptionInArray(i_Value, 22);
                                        } break;
                                    case OPTION_GAMEPAD2P_RELOAD:
                                        {
                                            SetOptionInArray(i_Value, 23);
                                        } break;
                                    case OPTION_GAMEPAD1P_START:
                                        {
                                            SetOptionInArray(i_Value, 24);
                                        } break;
                                    case OPTION_GAMEPAD2P_START:
                                        {
                                            SetOptionInArray(i_Value, 25);
                                        } break;
                                    case OPTION_GAMEPAD1P_CENTER:
                                        {
                                            SetOptionInArray(i_Value, 26);
                                        } break;
                                    case OPTION_GAMEPAD2P_CENTER:
                                        {
                                            SetOptionInArray(i_Value, 27);
                                        } break;                                    


                                    case OPTION_RESOLUTION:
                                        {
                                            SetOptionInArray(i_Value, 30);
                                        } break;
                                    case OPTION_FULLSCREEN:
                                        {
                                            SetOptionInArray(i_Value, 31);
                                        } break;
                                    case OPTION_FRAMERATE:
                                        {
                                            SetOptionInArray(i_Value, 32);
                                        } break;
                                    case OPTION_ALIASING:
                                        {
                                            SetOptionInArray(i_Value, 33);
                                        } break;
                                    case OPTION_TEXTURES:
                                        {
                                            SetOptionInArray(i_Value, 34);
                                        } break;
                                    case OPTION_COLORS:
                                        {
                                            SetOptionInArray(i_Value, 35);
                                        } break;
                                    case OPTION_LANGUAGE:
                                        {
                                            SetOptionInArray(i_Value, 37);
                                        } break;

                                    //

                                    case OPTION_VOLUME_BGM:
                                        {
                                            _VolumeBGM = (byte)i_Value;
                                        } break;
                                    case OPTION_VOLUME_SFX:
                                        {
                                            _VolumeSFX = (byte)i_Value;
                                        } break;
                                    case OPTION_VOLUME_VCE:
                                        {
                                            _VolumeVCE = (byte)i_Value;
                                        } break;
                                    case OPTION_DIFFICULTY:
                                        {
                                            _GameDifficulty = (byte)i_Value;
                                        } break;
                                    case OPTION_INITIAL_LIFE:
                                        {
                                            _InitialLife = (byte)i_Value;
                                        } break;
                                    case OPTION_MAX_LIFE:
                                        {
                                            _MaxLife = i_Value;
                                        } break;
                                    case OPTION_BLOOD_COLOR:
                                        {
                                            _BloodColor = (byte)i_Value;
                                        } break;
                                    case OPTION_VIOLENCE:
                                        {
                                            _Violence = (byte)i_Value;
                                        } break;
                                    case OPTION_ADVERTISE:
                                        {

                                        } break;
                                    case OPTION_CREDITS_TO_START:
                                        {
                                            _CreditsToStart = i_Value;
                                        } break;
                                    case OPTION_CREDITS_TO_CONTINUE:
                                        {
                                            _CreditsToContinue = i_Value;
                                        } break;
                                    case OPTION_FREEPLAY:
                                        {
                                            _Freeplay = (byte)i_Value;
                                        } break;
                                    case OPTION_CROSSHAIR_DESIGN:
                                        {
                                            _CrosshairDesign = (byte)i_Value;
                                        } break;
                                    case OPTION_CROSSHAIR_SPEED:
                                        {
                                            _CrosshairSpeed = (byte)i_Value;
                                        } break;
                                    case OPTION_HIDE_CROSSHAIR:
                                        {
                                            _HideCrosshairs = (byte)i_Value;
                                        } break;
                                    case OPTION_HIDE_CURSOR:
                                        {
                                            _HideCursor = (byte)i_Value;
                                        } break;
                                    case OPTION_DISABLE_PAUSE:
                                        {
                                            _DisablePause = (byte)i_Value;
                                        } break;

                                    default: break;
                                }
                            }
                            else
                            {
                                Logger.WriteLog("Configurator() => Error parsing value : " + Value + " for Key : " + Key);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception Ex)
            {
                Logger.IsEnabled = true;
                Logger.WriteLog("Configurator() => Can't read config data :\n" + AppDomain.CurrentDomain.BaseDirectory + CONF_FILENAME + " :\n\n" + Ex.Message.ToString());
                return false;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Globalization;
using Debugger;

namespace Hotd3Arcade_Launcher
{
    public class Hotd3Arcade_Launcher
    {
        private const string HOD3_CUSTOM_LOCAL_FOLDER = @"NVRAM\";
        private const string HOD3_DAT_FILENAME = "HOD3.DAT";
        private const string HOD3_SAVE_FILENAME = "SAVE.DAT";
        private const string MEMORY_DATA_FOLDER = "MemoryData";        

        private int _ProcessId = 0;
        private Process _Process;
        private IntPtr _Process_MemoryBaseAddress = IntPtr.Zero;
        private IntPtr _ProcessHandle = IntPtr.Zero;

        private Configurator _GameConfigurator;
        
        //Custom Data Bank
        private UInt32 _DataBank_Address = 0;
        private enum DataBank_Offset
        {
            CreditsKeyPushed = 0,
            BlinkTickCount = 4,  
            FrameTickCount = 8,
            CustomSpriteVisibility = 12,
            SkipIntro = 16,
            ShowIntro = 20,
            ShowReloadP1 = 24,
            ShowReloadP2 = 28,
            CustomDatFilePath = 50, //char[255]
            CustomSaveFilePath = 305, //char[255]
            CoinSoundFileName = 560,
            GameBaseDirectory = 815// char[255]
        }

        //Audio table for /fs/media sound is located at @5E56A8 :
        //4 Bytes => audi ID to use when calling the function 4A3D80
        //4 Bytes => memory address corresponding to the file path to load
        private UInt32 _SfxID_Reload = 0x0210FA9;
        //Coin sound is not in the table, that's why we will use the start SFX entry (not used anymore, at the title screen)
        private UInt32 _SfxID_ToUseForCoin = 0x10BA9;   //start02.aif
        
        //Custom functions that will need to be addresses by Assembly codecaves to be called
        private UInt32 _CustomDrawSprite1_Function_Address = 0;
        private UInt32 _CustomDrawSprite2_Function_Address = 0;
        private UInt32 _CustomDrawSpriteAll_Function_Address = 0;

        //Game Memory Data
        private UInt32 _EnterKeyPressed_Offset = 0x0003ECF48;
        private UInt32 _FlagFreeplay_Offset = 0x3B7E17;
        private UInt32 _Credits_Offset = 0x003B7DD0;
        private UInt32 _CreditsToStart_Offset = 0x003B7E1C;
        private UInt32 _CreditsToContinue_Offset = 0x003B7E1D;
        private UInt32 _ContinueTimerP1_Offset = 0x003B807C;
        private UInt32 _ContinueTimerP2_Offset = 0x003B82A8;
        private UInt32 _ContinueTimer_Offset = 0x003B8924;
        private UInt32 _CurrentSceneID_Offset = 0x003B8B74;
        private UInt32 _NextSceneID_Offset = 0x003B8780;
        private UInt32 _Player1GameStatus_Offset = 0x003B8038;
        private UInt32 _Player2GameStatus_Offset = 0x003B8264;
        private UInt32 _Player1Ammo_Offset = 0x003B8090;
        private UInt32 _Player2Ammo_Offset = 0x003B82BC;
        private UInt32 _DrawFs2SpritesFunction_Offset = 0x000A5EE0;
        private UInt32 _DrawFsSpritesFunction_Offset = 0x000A5EA0;
        private UInt32 _GetLocalTimeFunction_Offset = 0x0003D710;   //Caled in HOD3.DAT default values
        private UInt32 _CurrentPath_Offset = 0x0059C5F4;
        private UInt32 _SoundPlayerHandle_Offset = 0x005D2E18;
        private UInt32 _FltConstant_16f_Offset = 0x0001C26BC;
        private UInt32 _FltConstant_58f_Offset = 0x0001C28C4;
        private UInt32 _FltConstant_10f_Offset = 0x0001C2718;
        private UInt32 _FltConstant_46f_Offset = 0x0001C2938;
        private UInt32 _AllowStartGame = 0x00366748;
        private UInt32 _StartGameFromByteJmpTable_Offset = 0x0008E134;

        // Base Address of Loaded sprites ID
        private UInt32 _SprMenuExtraSprites_Offset = 0x199C58;
        //Used in Menu :
        //+599C58	:	EXIT (English)
        //+599C64	:	EXIT (german)                   Attract "reload"             
        //+599C70	:	EXIT (french)
        //+599C7C	:	EXIT (spanish)
        //+599C88	:	EXIT (italian)                  Attract "Shells"
        //+599D60	:	Sega trademark

        //Used in Options :
        //+599C94	:	Save game data (english)	
        //+599CA0	:	Save game data (german)		
        //+599CAC	:	Save game data (french)		
        //+599CB8	:	Save game data (spanish)	
        //+599CC4	:	Save game data (italian)	
        //+599CD0	:	Save game data (japanese)		CREDITS	
	
        //+599CDC	:	Load game data (english)	
        //+599CE8	:	Load game data (german)			FREEPLAY	
        //+599CF4	:	Load game data (french)		
        //+599D00	:	Load game data (spanish)	
        //+599D0C	:	Load game data (italian)	
        //+599D18	:	Load game data (japanese)		PRESS START

        //+599D24	:	Saving Game (english)		
        //+599D30	:	Saving Game (german)			INSERT_COIN
        //+599C3C	:	Saving Game (french)		
        //+599D48	:	Saving Game (spanish)		
        //+599D54	:	Saving Game (italian)		

        //[+599D60	:	Sega trademark]

        //+599D6C	:	Unable to load (english)		0
        //+599D78	:	Unable to load (german)			1
        //+599D84	:	Unable to load (french)			2
        //+599D90	:	Unable to load (spanish)		3
        //+599D9C	:	Unable to load (italian)		4
        //+599DA8	:	Unable to load (japanese)		5

        //+599DB4	:	Crosshair tooltip (english)		6
        //+599DC0	:	Crosshair tooltip (german)		7
        //+599DCC	:	Crosshair tooltip (french)		8
        //+599DD8	:	Crosshair tooltip (spanish)		9
        //+599DE4	:	Crosshair tooltip (italian)		Title Trademark
        //+599DF0	:	Crosshair tooltip (japanese)	Title 2

        //+599DFC	:	END (english)
        //+599E08	:	END (german)
        //+599E14	:	END (french)
        //+599E20	:	END (spanish)
        //+599E2C	:	END (italian)
        private enum SprMenuExtraSprite
        {
            AttractReload = 0x0C,
            AttractShells = 0x30,
            Credits = 0x78,
            Freeplay = 0x90,
            PressStart = 0xC0,
            InsertCoin = 0xD8,
            Num0 = 0x114,
            //+0x0C for eaxh number untill 9            
            Logo = 0x198,
            Trademark = 0x18C
        }
        //For our custom INSERT COINS / PRESS START sprites, fixed values (screen is 640x480)
        //Origin : center of the sprite (so that different lenght/language are still centered)
        private const float _Players_UpperSpriteInfo_Y = 426.0f;
        private const float _Player1_SpriteInfo_X = 142.0f;
        private const float _Player2_SpriteInfo_X = 496.0f;
        //For our custom FREEPLAY / CREDITS sprites, fixed values (screen is 640x480)
        //Origin : center of the sprite (so that different lenght/language are still centered)
        private const float _Players_LowerSpriteInfo_Y = 444.0f;
        //FREEPLAY will be centered (only 1 sprite), but CREDITS and DIGITS need adjustments (and more according to the CREDITS lenght in language)
        private float _Players_LowerSpriteInfo_OffsetX1 = -15.0f; //CREDITS offset
        private float _Players_LowerSpriteInfo_OffsetX2 = 32.0f; //Digit#1 offset

        //Memory Hacks
        private UInt32 _IntroLogoChange_Offset1 = 0x00076383;
        private UInt32 _IntroLogoChange_Offset2 = 0x000763F9;
        private UInt32 _IntroLogoChange_Offset3 = 0x00076505;
        private UInt32 _AppDataFolderAndRegistry_SaveFile_Offset = 0x0003CF3F;
        private UInt32 _AppDataFolderAndRegistry_DatFile_Offset = 0x0003CB6F;
        private UInt32 _RegistryWritePlayed_Offset = 0x000B2E26;
        private UInt32 _RegistryCheckPlayed_Offset = 0x000B1E38;
        private UInt32 _RegistryReadPath = 0x000B1EB8;
        private UInt32 _FreeplayInit_Offset = 0x0003E0F1;
        private UInt32 _OverrideDatFile_CheckName_Offset = 0x0003CA26;
        private UInt32 _OverrideDatFile_ReadName_Offset = 0x0003CA42;
        private UInt32 _OverrideDatFile_WriteName_Offset = 0x0003D38D;
        private UInt32 _OverrideSaveFile_WriteName_Offset = 0x0003D621;
        private UInt32 _OverrideSaveFile_CheckName_Offset = 0x0003D4B0;
        private UInt32 _OverrideSaveFile_ReadName_Offset = 0x0003D4C8;
        private UInt32 _ReplaceTitleLogo_Offset = 0x000A8D3D;
        private UInt32 _DisableTimeAttackAttractMode_Offset = 0x000A9120;
        private UInt32 _DisableTimeAttackRankingDisplay_Offset = 0x0009B106;
        private UInt32 _EnableContinueScreen_Offset = 0x0003E549;
        private UInt32 _HideCrosshairs_Offset = 0x0008E989;
        private UInt32 _MaxLife_Offset = 0x0008E39E;
        private UInt32 _MainMenuAutoValidate_Offset = 0x00077A19;
        private UInt32 _DisableEnterKeyUp_Offset = 0x000B2A52;
        private UInt32 _DisableEnterKeyDown_Offset = 0x000B29A2;
        private UInt32 _DisableSetFocus_Offset = 0x000B29C7;
        private UInt32 _DisableExitConfirm_Offset = 0x000B2AB2;
        private UInt32 _QuitGameProc_Offset = 0x000B2A30;
        private UInt32 _OverrideSprMenuExtrasSize_Offset = 0x00077737;
        private UInt32 _OverrideSprLogoSize_Offset = 0x00075F3F;
        private UInt32 _FastReload_Offset = 0x00204DCC;
        private UInt32 _DisableInGamePause_v1_Offset = 0x008B5A0; 
        private UInt32 _DisableInGamePause_v2_Offset = 0x008C2B3;
        private UInt32 _StartGameFromTitle_Offset1 = 0x0003E4F2;
        private UInt32 _StartGameFromTitle_Offset2 = 0x0008D9A4;
        private UInt32 _StartGameFromTitleSwitchCase_Offset = 0x0008E01E;

        private NopStruct _Nop_RegQuerryValueExA1 = new NopStruct(0x000B25BA, 6);
        private NopStruct _Nop_RegQuerryValueExA2 = new NopStruct(0x000B25EC, 6);
        private NopStruct _Nop_MenuScreen_Background = new NopStruct(0x00077B3F, 5);
        private NopStruct _Nop_MenuScreen_Logo = new NopStruct(0x00077B6D, 5);
        private NopStruct _Nop_MenuScreen_Copyright = new NopStruct(0x00077B9E, 5);
        private NopStruct _Nop_MenuScreen_ItemText = new NopStruct(0x00077D87, 5);
        private NopStruct _Nop_MenuScreen_Arrow1 = new NopStruct(0x00077DB8, 5);
        private NopStruct _Nop_MenuScreen_Arrow2 = new NopStruct(0x00077DE2, 5);
        private NopStruct _Nop_MenuScreen_Arrow3 = new NopStruct(0x00077E13, 5);
        private NopStruct _Nop_MenuScreen_Arrow4 = new NopStruct(0x00077E41, 5);
        private NopStruct _Nop_CreditsInit_1 = new NopStruct(0x0003E660, 6);
        private NopStruct _Nop_CreditsInit_2 = new NopStruct(0x0003E610, 2);
        private NopStruct _Nop_CreditsInit_3 = new NopStruct(0x0003E640, 2);
        private NopStruct _Nop_CreditsInit_4 = new NopStruct(0x0003E600, 7);
        private NopStruct _Nop_CreditsToStartAndContinueInit = new NopStruct(0x0003E0E3, 10);    //CreditsToStart & CreditsToContinue
        private NopStruct _Nop_SfxPlayStart = new NopStruct(0x0008E005, 16);
        private NopStruct _Nop_NoAutoReload_1 = new NopStruct(0x0008DEDB, 3);        
        private NopStruct _Nop_Arcade_Mode_Display = new NopStruct(0x0008FD29, 2);    
        private NopStruct _Nop_ConfirmExitGame = new NopStruct(0x000B2AB2, 12);

        private InjectionStruct _WndProcCreditsBtnUp_Injection = new InjectionStruct(0x000B2A4B, 7);
        private InjectionStruct _WndProcCreditsBtnDown_Injection = new InjectionStruct(0x000B299B, 7);
        private UInt32 _WndProcCredits_CalledFunctionOffset = 0x000A3D80;
        private InjectionStruct _ForceCoinSoundEffect_Injection = new InjectionStruct(0x000A3FC8, 5);
        private InjectionStruct _OverideLoadingHod3Dat_Injection = new InjectionStruct(0x0003CAE9, 6);
        private InjectionStruct _OverideDefaultHod3Dat_Injection1 = new InjectionStruct(0x0003D30D, 5);
        private InjectionStruct _OverideDefaultHod3Dat_Injection2 = new InjectionStruct(0x0003D331, 5);
        private InjectionStruct _OverideDefaultHod3Dat_Injection3 = new InjectionStruct(0x0003D355, 5);
        private InjectionStruct _ChangeTitleLogo_Injection = new InjectionStruct(0x000A8D95, 5);
        private InjectionStruct _ChangeTitleScreenSprites_Injection = new InjectionStruct(0x000A8E20, 5);
        private InjectionStruct _BlockTitleStartIfNoCredits_Injection = new InjectionStruct(0x000B4C65, 5);
        private InjectionStruct _MenuScreenCursor_Injection = new InjectionStruct(0x00032BDE, 9);
        private InjectionStruct _ReturnToMenu_Injection = new InjectionStruct(0x00032BB0, 5);
        private InjectionStruct _InitLifeLimitToMax_Injection = new InjectionStruct(0x0008DC08, 7);
        private InjectionStruct _LoopThreadVariousThings_Injection = new InjectionStruct(0x0003E193, 6);
        private InjectionStruct _OverrideSprTitleExtras_Injection = new InjectionStruct(0x000A8899, 5);
        private InjectionStruct _OverrideSprMenuExtras_Injection = new InjectionStruct(0x00077759, 5);
        private InjectionStruct _OverrideSprLogo_Injection = new InjectionStruct(0x00075F5f, 5);
        private InjectionStruct _OverrideRegistryRead_Injection = new InjectionStruct(0x000B24E1, 6);
        private InjectionStruct _AddCutScenesInfoSprites_Injection = new InjectionStruct(0x0003E1ED, 5);
        private InjectionStruct _ReplaceFreeplaySpriteInGame_Injection = new InjectionStruct(0x0003E212, 5);
        private InjectionStruct _ReplaceCreditsSpriteInGame_Injection = new InjectionStruct(0x0003E25E, 5);
        private InjectionStruct _ReplaceCreditsDigit1SpriteInGame_Injection = new InjectionStruct(0x0003E2C3, 5);
        private InjectionStruct _ReplaceCreditsDigit2SpriteInGame_Injection = new InjectionStruct(0x0003E30B, 5);
        private InjectionStruct _ReplacePressStartButtonSpriteInGame_Injection = new InjectionStruct(0x0003E1CC, 5);
        private InjectionStruct _StartGameFromTitle_Injection = new InjectionStruct(0x0008DFED, 7);
        private InjectionStruct _AddReloadSfx_Injection1 = new InjectionStruct(0x0008DF1E, 5);
        private InjectionStruct _AddReloadSfx_Injection2 = new InjectionStruct(0x0008DED4, 7);
        private InjectionStruct _ReplaceAttractModeSprites_Injection = new InjectionStruct(0x000A960A, 5);


        //MD5 check of target binaries, may help to know if it's the wrong version or not compatible
        protected Dictionary<string, string> _KnownMd5Prints;
        protected String _TargetProcess_Md5Hash = string.Empty;

        private enum Hotd3_Scene
        {
            Sega_Logo = 0,
            MainMenu,
            MainMenu2,
            Sega_Logo2,
            IntroductionMovie,
            MainTitleScreen,
            AttractMode,
            ScoringScreen,
            ScoringScreen2
        }

        Debugger.QuickDebugger _Qdb;

        /// <summary>
        /// Entry Point
        /// </summary>
        /// <param name="EnableLogs"></param>
        public Hotd3Arcade_Launcher(bool EnableLogs)
        {
            Logger.InitLogFileName();
            Logger.IsEnabled = EnableLogs;

            _GameConfigurator = new Configurator();
            if (!_GameConfigurator.ReadConf())
            {
                Logger.IsEnabled = true;
                Logger.WriteLog("Hotd3Arcade_Launcher() => No config file found. Abording...");
                Environment.Exit(0);
            }

            if (_GameConfigurator.Language == 3)
            {
                _Players_LowerSpriteInfo_OffsetX2 = _Players_LowerSpriteInfo_OffsetX2 + 10.0f;
            }

            _KnownMd5Prints = new Dictionary<String, String>();
            _KnownMd5Prints.Add("hod3pc SEGA Windows", "4bf19dcb7f0182596d93f038189f2301");
            _KnownMd5Prints.Add("hod3pc RELOADED cracked", "3a4501d39bbb7271712421fb992ad37b");
            _KnownMd5Prints.Add("hod3pc REVELATION No-CD", "b8af47f16d5e43cddad8df0a6fdb46f5");
            _KnownMd5Prints.Add("hod3pc MYTH Release", "0228818e9412fc218fcd24bfd829a5a0");
            _KnownMd5Prints.Add("hod3pc TEST", "733da4e3cfe24b015e5f795811d481e6");
            _KnownMd5Prints.Add("hod3pc Unknown Release #1", "51dd72f83c0de5b27c0358ad11e2a036");
            _KnownMd5Prints.Add("hod3pc Unknown Release #2", "e4819dcf2105b85a7e7bc9dc66159f5c");   

            if (!System.IO.Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + HOD3_CUSTOM_LOCAL_FOLDER))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + HOD3_CUSTOM_LOCAL_FOLDER);
        }

        /// <summary>
        /// Create the process with DEBUG attributes, so that we can stop it to inject the code, and then resume it
        /// Inspired from a hand-made debugger https://www.codeproject.com/Articles/43682/Writing-a-basic-Windows-debugger
        /// </summary>
        public void RunGame()
        {
            _Qdb = new QuickDebugger(_GameConfigurator.GameFilePath);
            _Qdb.OnDebugEvent += new Debugger.QuickDebugger.DebugEventHandler(Qdb_OnDebugEvent);
            _Qdb.StartProcess();
        }
        private void Qdb_OnDebugEvent(object sender, Debugger.DebugEventArgs e)
        {
            switch (e.Dbe.dwDebugEventCode)
            {
                case DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
                    {
                        Logger.WriteLog("RunGame() => Process created");
                        _Qdb.ContinueDebugEvent();
                    } break;


                case DebugEventType.CREATE_THREAD_DEBUG_EVENT:
                    {
                        DEBUG_EVENT.CREATE_THREAD_DEBUG_INFO ti = new DEBUG_EVENT.CREATE_THREAD_DEBUG_INFO();
                        ti = e.Dbe.CreateThread;
                        Logger.WriteLog("Thread 0x" + ti.hThread.ToString("X8") + " (Id: " + e.Dbe.dwThreadId.ToString() + ") created");
                        _Qdb.ContinueDebugEvent();
                    } break;


                //The game has a breakpoint installed at start (!), we can use it to search for information, block the process to insert our code
                case DebugEventType.EXCEPTION_DEBUG_EVENT:
                    {
                        DEBUG_EVENT.EXCEPTION_DEBUG_INFO Ex = new DEBUG_EVENT.EXCEPTION_DEBUG_INFO();
                        Ex = e.Dbe.Exception;

                        if (Ex.ExceptionRecord.ExceptionCode == QuickDebugger.STATUS_BREAKPOINT)
                        {
                            Logger.WriteLog("RunGame() => Breakpoint reached !");
                            Process p = Process.GetProcessById(e.Dbe.dwProcessId);
                            _Process = p;
                            _ProcessId = _Process.Id;
                            _ProcessHandle = _Process.Handle;
                            _Process_MemoryBaseAddress = _Process.MainModule.BaseAddress;
                            Logger.WriteLog("RunGame() => Process ID: " + _ProcessId.ToString());
                            Logger.WriteLog("RunGame() => Process Memory Base Address: 0x" + _Process_MemoryBaseAddress.ToString("X8"));
                            Logger.WriteLog("RunGame() => Process Handle: 0x" + _ProcessHandle.ToString("X8"));

                            CheckExeMd5();
                            ReadGameDataFromMd5Hash();
                            Apply_Hacks();

                            Logger.WriteLog("RunGame() => Hack complete, leaving the game to run on its own now....");
                            _Qdb.DetachDebugger();
                            _Qdb.ContinueDebugEvent();                            
                        }
                    } break;

                default:
                    {
                        _Qdb.ContinueDebugEvent();
                    } break;
            }
        }

        /// <summary>
        /// Creating the Process without DEBUG attributes can allow another debugger to go in and analyse what's going on
        /// </summary>
        public void Run_Game_Debug()
        {
            _Process = new Process();
            _Process.StartInfo.FileName = _GameConfigurator.GameFilePath;
            _Process.Start();

            try
            {
                ProcessTools.SuspendProcess(_Process);               
                _ProcessId = _Process.Id;                
                _ProcessHandle = _Process.Handle;
                _Process_MemoryBaseAddress = _Process.MainModule.BaseAddress;
                Apply_Hacks();

                ProcessTools.ResumeProcess(_Process);
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception )
            {
            }
        }

        #region Hacks

        public void Apply_Hacks()
        {
            //Start Modding:
            //Create functions and storage for later
            Create_DataBank();
            Mod_CreateDrawSprite1_Function(0.25f, 0.25f, 0x0A);
            Mod_CreateDrawSprite2_Function(0.25f, 0.25f, 0x0A);
            Mod_CreateDrawSpritesAll_Function();

            //
            Mod_RegistryChecks();

            if (_GameConfigurator.HideCursor == 1)
                Mod_HideCursor();

            Mod_OverrideLoadingHod3DatFile();
            Mod_OverrideDefaultHod3DatFile(_OverideDefaultHod3Dat_Injection1);
            Mod_OverrideDefaultHod3DatFile(_OverideDefaultHod3Dat_Injection2);
            Mod_OverrideDefaultHod3DatFile(_OverideDefaultHod3Dat_Injection3);
            Mod_OverrideSaveFile();
            Mod_WndProcLoop();
            Mod_WndProcLoop_CreditsButton();
            Mod_ForceCoinSoundEffect();
            Mod_CreditsHandling();
            Mod_InjectSprMenuExtras();
            Mod_InjectSprLogo();
            Mod_AddLotOfThingsIntoQuickThread();

            Mod_IntroductionLoop();
            Mod_TitleScreen();
            Mod_ReplaceAttractModeSprites();
           
            //Mod_BlockTitleIfNoCredits();
            Mod_StartGameFromTitleScreen(); //Replace the old hack from above, cleaner way.
            Mod_DisableTimeAttackAttractMode();

            //Mod_RemoveMenu_Screen();    //Not Needed if we use Mod_StartGameFromTitleScreen(); 
            Mod_ReturnToMenu();

            if (_GameConfigurator.DisablePause == 1)
                Mod_DisableInGamePause_v2();

            Mod_SetMaxLife();
            Mod_FastReload();
            Mod_HideGuns();
            Mod_NoAutoReload();
            Mod_AddReloadEffects();
            Mod_ReplaceFreeplaySpriteInGame();
            Mod_ReplaceCreditsSpriteInGame();
            Mod_ReplaceCreditsDigit1SpriteInGame();
            Mod_ReplaceCreditsDigit2SpriteInGame();
            Mod_ReplacePushStartButtonSpriteInGame();
            Mod_ForceContinueDispay();
            Mod_ConfirmExitGame();

            if (_GameConfigurator.HideCrosshairs == 1)
                Mod_HideCrosshairs();
        }

        //Create a memory zone to store data to pass between Different codecaves or Hacks
        private void Create_DataBank()
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x1500);
            _DataBank_Address = CaveMemory.CaveAddress;

            Logger.WriteLog("Create_DataBank() => Adding DataBank codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            WriteByte(_DataBank_Address + (uint)DataBank_Offset.SkipIntro, 0x01);
            WriteByte(_DataBank_Address + (uint)DataBank_Offset.ShowIntro, 0x01);
            WriteByte(_DataBank_Address + (uint)DataBank_Offset.CreditsKeyPushed, 0x00);

            String GameBaseDirectory = @"C:\Program Files (x86)\SEGA\THE HOUSE OF THE DEAD3_EU";
            WriteBytes(_DataBank_Address + (uint)DataBank_Offset.GameBaseDirectory, ASCIIEncoding.ASCII.GetBytes(GameBaseDirectory));

            String Hod3DatPath = AppDomain.CurrentDomain.BaseDirectory + HOD3_CUSTOM_LOCAL_FOLDER + HOD3_DAT_FILENAME;
            WriteBytes(_DataBank_Address + (uint)DataBank_Offset.CustomDatFilePath, ASCIIEncoding.ASCII.GetBytes(Hod3DatPath));

            String Hod3SavePath = AppDomain.CurrentDomain.BaseDirectory + HOD3_CUSTOM_LOCAL_FOLDER + HOD3_SAVE_FILENAME;
            WriteBytes(_DataBank_Address + (uint)DataBank_Offset.CustomSaveFilePath, ASCIIEncoding.ASCII.GetBytes(Hod3SavePath));

            String CoinSoundFileName = @"..\media\coin002.aif";
            WriteBytes(_DataBank_Address + (uint)DataBank_Offset.CoinSoundFileName, Encoding.ASCII.GetBytes(CoinSoundFileName));           
        }
                 
        // Remove all registry functions : no writing nor reading
        // Original config option read in registry will be injected by our own loader
        // Registry checks also removed
        private void Mod_RegistryChecks()
        {
            /*** Disable Registry writing, we don't need it ***/
            //43CF3F => Shunt whole procedure to create documents and settigns game folder and write register
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _AppDataFolderAndRegistry_SaveFile_Offset, new byte[] { 0xE9, 0x55, 0x03, 0x00, 0x00, 0x90 });
            //Just the registry write :
            //WriteByte((UInt32)_Process_MemoryBaseAddress + 0x3CE9D, 0xEB);

            //43CF3F => Shunt whole procedure to create documents and settigns game folder and write register
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _AppDataFolderAndRegistry_DatFile_Offset, new byte[] { 0xE9, 0x55, 0x03, 0x00, 0x00, 0x90 });
            //Just the registry :
            //WriteByte((UInt32)_Process_MemoryBaseAddress + 0x3D26D, 0xEB);

            //Disable writing Played=0 at program exit
            WriteByte((UInt32)_Process_MemoryBaseAddress + _RegistryWritePlayed_Offset, 0xEB);

            //Disable RegOpenKey error if registry not existing
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _RegistryCheckPlayed_Offset - 0xAC, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });

            //Remove registry check "Played = 1" at start 
            WriteByte((UInt32)_Process_MemoryBaseAddress + _RegistryCheckPlayed_Offset - 0x76, 0xEB);   //check if QuerryValue success
            WriteByte((UInt32)_Process_MemoryBaseAddress + _RegistryCheckPlayed_Offset, 0xEB);          //check if value != 0
                                

            //Remove the call for RegQueryValue do get path, and use our data for the call to SetCurrentDirectory()
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _RegistryReadPath, new byte[] { 0x83, 0xC4, 0x14 });
            string s = _GameConfigurator.GameFilePath.Substring(0, _GameConfigurator.GameFilePath.IndexOf(@"\exe\hod3pc.exe"));
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _CurrentPath_Offset, ASCIIEncoding.ASCII.GetBytes(s));


            //Replacing the options values from registry by our own
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            int offset = 0x250;
            for (int i = 0; i < _GameConfigurator.RegValues.Length; i++)
            {
                CaveMemory.Write_StrBytes("C7 86");
                if (i == 4 || i == 30)
                    offset += 0x08;
                CaveMemory.Write_Bytes(BitConverter.GetBytes(offset));
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_GameConfigurator.RegValues[i]));
                offset += 0x04;
            }
            //mov eax,[esi+000002DC]
            CaveMemory.Write_StrBytes("8B 86 DC 02 00 00");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _OverrideRegistryRead_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_RegistryChecks() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _OverrideRegistryRead_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _OverrideRegistryRead_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            //Disable the last 2 readings, already set by the previous codecave
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_RegQuerryValueExA1);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_RegQuerryValueExA2);
        }        

        //Modifying the read buffer from the DAT file
        //If need be, the data is finally saved in fixed memory location, pointer in 0x9D3008
        private void Mod_OverrideLoadingHod3DatFile()
        {
            //Force the gameto use our own DAT file, while letting the original path in registry
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _OverrideDatFile_CheckName_Offset + 1, BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomDatFilePath));
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _OverrideDatFile_ReadName_Offset + 1, BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomDatFilePath));
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _OverrideDatFile_WriteName_Offset + 1, BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomDatFilePath));

            //Freeplay WORD in memory is set by confing only before a game is starting
            //To make our custom credits handling work at title, we need to set it when it's initialized 
            if (_GameConfigurator.Freeplay == 1)
            {
                WriteBytes((UInt32)_Process_MemoryBaseAddress + _FreeplayInit_Offset, new byte[] {0xC6, 0x05, 0x17, 0x7E, 0x7B, 0x00, 0x01, 0x88, 0x0D, 0x13, 0x7E, 0x7B, 0x00, 0x89, 0x08, 0x5F, 0xC2, 0x04, 0x00 });
            }

            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //mov [edx+000000F0], BGM Volume
            CaveMemory.Write_StrBytes("C6 82 F0 00 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.VolumeBGM);
            //mov [edx+000000F1], SFX Volume
            CaveMemory.Write_StrBytes("C6 82 F1 00 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.VolumeSFX);
            //mov [edx+000000F2], Vopice Volume
            CaveMemory.Write_StrBytes("C6 82 F2 00 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.VolumeVCE);

            //mov [edx+00000184], Difficulty
            CaveMemory.Write_StrBytes("C6 82 84 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.GameDifficulty);
            //mov [edx+00000186], InitialLifes
            CaveMemory.Write_StrBytes("C6 82 86 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.InitialLife);
            if (_GameConfigurator.Freeplay == 1)
            {
                //mov [edx+00000187], Freeplay
                CaveMemory.Write_StrBytes("C6 82 87 01 00 00 FF");
            }
            else
            {
                //mov [edx+00000187], 01
                CaveMemory.Write_StrBytes("C6 82 87 01 00 00 05");
            }
            //mov [edx+00000188], Violence
            CaveMemory.Write_StrBytes("C6 82 88 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.Violence);
            //mov [edx+00000189], BloodColor
            CaveMemory.Write_StrBytes("C6 82 89 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.BloodColor);
            CaveMemory.Write_StrBytes("C6 82 8A 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.BloodColor);
            //mov [edx+00000190], Crosshairdesign
            CaveMemory.Write_StrBytes("C6 82 90 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.CrosshairDesign);
            //mov [edx+00000191], CrosshairSpeed
            CaveMemory.Write_StrBytes("C6 82 91 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.CrosshairSpeed);
            // mov [edx+18],si
            CaveMemory.Write_StrBytes("66 89 72 18");
            //ebx,ebx
            CaveMemory.Write_StrBytes("85 DB");

            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _OverideLoadingHod3Dat_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_OverrideLoadingHod3DatFile() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _OverideLoadingHod3Dat_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _OverideLoadingHod3Dat_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //If no DAT file is found, the game is loading default values
        //We will put our own inside this part
        //The function is called by different places, so we will modify our values before the return is called
        //There are 3 return in this function...so 3 codecave do do
        private void Mod_OverrideDefaultHod3DatFile(InjectionStruct MyInjection)
        {
            //Freeplay WORD in memory is set by confing only before a game is starting
            //To make our custom credits handling work at title, we need to set it when it's initialized 
            if (_GameConfigurator.Freeplay == 1)
            {
                WriteBytes((UInt32)_Process_MemoryBaseAddress + _FreeplayInit_Offset, new byte[] { 0xC6, 0x05, 0x17, 0x7E, 0x7B, 0x00, 0x01, 0x88, 0x0D, 0x13, 0x7E, 0x7B, 0x00, 0x89, 0x08, 0x5F, 0xC2, 0x04, 0x00 });
            }

            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //Call GeTLocalTime Sub
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _GetLocalTimeFunction_Offset);
            //mov [edi+000000F0], BGM Volume
            CaveMemory.Write_StrBytes("C6 87 F0 00 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.VolumeBGM);
            //mov [edi+000000F1], SFX Volume
            CaveMemory.Write_StrBytes("C6 87 F1 00 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.VolumeSFX);
            //mov [edi+000000F2], Vopice Volume
            CaveMemory.Write_StrBytes("C6 87 F2 00 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.VolumeVCE);

            //mov [edi+00000184], Difficulty
            CaveMemory.Write_StrBytes("C6 87 84 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.GameDifficulty);
            //mov [edi+00000186], InitialLifes
            CaveMemory.Write_StrBytes("C6 87 86 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.InitialLife);
            if (_GameConfigurator.Freeplay == 1)
            {
                //mov [edi+00000187], Freeplay
                CaveMemory.Write_StrBytes("C6 87 87 01 00 00 FF");
            }
            //mov [edi+00000188], Violence
            CaveMemory.Write_StrBytes("C6 87 88 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.Violence);
            //mov [edi+00000189], BloodColor
            CaveMemory.Write_StrBytes("C6 87 89 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.BloodColor);
            CaveMemory.Write_StrBytes("C6 87 8A 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.BloodColor);
            //mov [edi+00000190], Crosshairdesign
            CaveMemory.Write_StrBytes("C6 87 90 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.CrosshairDesign);
            //mov [edi+00000191], CrosshairSpeed
            CaveMemory.Write_StrBytes("C6 87 91 01 00 00");
            CaveMemory.Write_Byte(_GameConfigurator.CrosshairSpeed);
            

            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + MyInjection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_OverrideDefaultHod3DatFile() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + MyInjection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + MyInjection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //Instead of saving to the file from the game computed filename, directly putting our own path before CreateFileA call
        //Same thing for the "check if exists" call and when the game wnats to read
        private void Mod_OverrideSaveFile()
        {
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _OverrideSaveFile_WriteName_Offset + 1, BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomSaveFilePath));
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _OverrideSaveFile_CheckName_Offset + 1, BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomSaveFilePath));
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _OverrideSaveFile_ReadName_Offset + 1, BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomSaveFilePath));
        }

        //Keyboard keys handling (disabling [RETURN]
        //SetFocus handling (remove pause when lost focus)
        private void Mod_WndProcLoop()
        {
            //Disabling [RETURN] keyboard key
            WriteByte((UInt32)_Process_MemoryBaseAddress + _DisableEnterKeyUp_Offset, 0xEB);
            WriteByte((UInt32)_Process_MemoryBaseAddress + _DisableEnterKeyDown_Offset, 0xEB);

            //Disabling Lost focus = pause
            WriteByte((UInt32)_Process_MemoryBaseAddress + _DisableSetFocus_Offset, 0xEB);
        }

        //Modifying the WM_KEYDOWN check to add [5] keys to add credits, play sound and reset continue timer
        //Also add [ESC] to quit
        //And modify KeyUp to prevent button always down
        private void Mod_WndProcLoop_CreditsButton()
        { 
            Codecave CaveMemoryBtnDown = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemoryBtnDown.Open();
            CaveMemoryBtnDown.Alloc(0x800);
            List<byte> Buffer;
            //mov edx,[esp+18]
            CaveMemoryBtnDown.Write_StrBytes("8B 54 24 18");
            //cmp edx, CreditsKey
            CaveMemoryBtnDown.Write_StrBytes("83 FA 35");  //[5] VirtualKeyCode
            //jne Next
            CaveMemoryBtnDown.Write_StrBytes("75 64");
            //cmp [CreditKeyPRessed], 0
            CaveMemoryBtnDown.Write_StrBytes("83 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CreditsKeyPushed));
            CaveMemoryBtnDown.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnDown.Write_StrBytes("00");
            //jne Next
            CaveMemoryBtnDown.Write_StrBytes("75 5B");
            //cmp dword ptr _CurrentSceneID_Offset,03
            CaveMemoryBtnDown.Write_StrBytes("83 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CurrentSceneID_Offset));
            CaveMemoryBtnDown.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnDown.Write_StrBytes("03");
            //je Next
            CaveMemoryBtnDown.Write_StrBytes("74 52");
            //cmp Credits,00000099
            CaveMemoryBtnDown.Write_StrBytes("81 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            CaveMemoryBtnDown.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnDown.Write_StrBytes("99 00 00 00");
            //jnl Next
            CaveMemoryBtnDown.Write_StrBytes("7D 46");
            //mov [CreditsKeyPushed], 1
            CaveMemoryBtnDown.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CreditsKeyPushed));
            CaveMemoryBtnDown.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnDown.Write_StrBytes("01 00 00 00");
            //add Credits, 1
            CaveMemoryBtnDown.Write_StrBytes("83 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            CaveMemoryBtnDown.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnDown.Write_StrBytes("01");
            //mov [_ContinueTimerP1_Offset],00009FFF
            CaveMemoryBtnDown.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _ContinueTimerP1_Offset));
            CaveMemoryBtnDown.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnDown.Write_StrBytes("FF 9F 00 00");
            //mov [_ContinueTimerP2_Offset],00009FFF
            CaveMemoryBtnDown.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _ContinueTimerP2_Offset));
            CaveMemoryBtnDown.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnDown.Write_StrBytes("FF 9F 00 00");
            //mov [_ContinueTimer_Offset],00009FFF
            CaveMemoryBtnDown.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _ContinueTimer_Offset));
            CaveMemoryBtnDown.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnDown.Write_StrBytes("FF 9F 00 00");
            //push eax
            CaveMemoryBtnDown.Write_StrBytes("50");
            //push ecx
            CaveMemoryBtnDown.Write_StrBytes("51");
            //push edx
            CaveMemoryBtnDown.Write_StrBytes("52");
            //push 00
            CaveMemoryBtnDown.Write_StrBytes("6A 00");
            //push 00010BA9 = ID for Start sound
            CaveMemoryBtnDown.Write_StrBytes("68 A9 0B 01 00");
            //push 9D2E18
            CaveMemoryBtnDown.Write_StrBytes("68");
            CaveMemoryBtnDown.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SoundPlayerHandle_Offset));
            //call 4A3D80
            CaveMemoryBtnDown.Write_call((UInt32)_Process_MemoryBaseAddress + _WndProcCredits_CalledFunctionOffset);
            //pop eax
            CaveMemoryBtnDown.Write_StrBytes("5A");
            //pop ecx
            CaveMemoryBtnDown.Write_StrBytes("59");
            //pop edx
            CaveMemoryBtnDown.Write_StrBytes("58");
            //cmp edx, 0x1B
            CaveMemoryBtnDown.Write_StrBytes("83 FA 1B");
            //jne Next
            CaveMemoryBtnDown.Write_StrBytes("75 05");
            //jmp QuitApp
            CaveMemoryBtnDown.Write_jmp((UInt32)_Process_MemoryBaseAddress + _QuitGameProc_Offset);
            //cmp edx, 0D
            CaveMemoryBtnDown.Write_StrBytes("83 FA 0D");
            //jmp return
            CaveMemoryBtnDown.Write_jmp((UInt32)_Process_MemoryBaseAddress + _WndProcCreditsBtnDown_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_WndProcLoop_CreditsButton() => Adding BtnDown Codecave at : 0x" + CaveMemoryBtnDown.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemoryBtnDown.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _WndProcCreditsBtnDown_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _WndProcCreditsBtnDown_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);


            Codecave CaveMemoryBtnUp = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemoryBtnUp.Open();
            CaveMemoryBtnUp.Alloc(0x800);
            //cmp edx, CreditsKey
            CaveMemoryBtnUp.Write_StrBytes("83 FB 35");  //[5] VirtualKeyCode
            //jne Next
            CaveMemoryBtnUp.Write_StrBytes("75 0A");
            //mov [CreditsKeyPushed], 0
            CaveMemoryBtnUp.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CreditsKeyPushed));
            CaveMemoryBtnUp.Write_Bytes(Buffer.ToArray());
            CaveMemoryBtnUp.Write_StrBytes("00 00 00 00");
            //mov edx,[esp+18]
            CaveMemoryBtnUp.Write_StrBytes("8B 54 24 18");
            //cmp edx,0D
            CaveMemoryBtnUp.Write_StrBytes("83 FA 0D");
            //jmp return
            CaveMemoryBtnUp.Write_jmp((UInt32)_Process_MemoryBaseAddress + _WndProcCreditsBtnUp_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_WndProcLoop_CreditsButton() => Adding BtnUp Codecave at : 0x" + CaveMemoryBtnUp.CaveAddress.ToString("X8"));

            //Code injection
            bytesWritten = 0;
            jumpTo = CaveMemoryBtnUp.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _WndProcCreditsBtnUp_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _WndProcCreditsBtnUp_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //Change the sound effect file played when using the Credits button
        //The game does dot allow to play all sounf files on disk, so we choose to play the start sound effect ID
        private void Mod_ForceCoinSoundEffect()
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            Buffer = new List<byte>();
            //push C8
            CaveMemory.Write_StrBytes("68 C8 00 00 00");
            //cmp ebp, _SfxID_ToUseForCoin
            CaveMemory.Write_StrBytes("81 FD");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SfxID_ToUseForCoin));
            //jne NextCheck
            CaveMemory.Write_StrBytes("75 05");
            //mov ebx, DataBank.CoinSoundFile
            CaveMemory.Write_StrBytes("BB");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CoinSoundFileName));
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ForceCoinSoundEffect_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_ForceCoinSoundEffect() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ForceCoinSoundEffect_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ForceCoinSoundEffect_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //Replace the original spr_menuextras.zip by the one we have in memory, at loading time
        //First step is to change the malloc size called to read the file
        //Second step is to copy our own data in the malloc space reserved
        //To acces the sprites, use following EDI values when calling Sub_4A5AC0() :
        //0x00599E38 = SEGA.dds
        //0x00599E44 = SEGA_WOW.dds
        private void Mod_InjectSprLogo()
        {
            byte[] b_MyFile = Properties.Resources.spr_logo;

            //Store zip file in game memory 
            Codecave SprMemoryDump = new Codecave(_Process, _Process.MainModule.BaseAddress);
            SprMemoryDump.Open();
            SprMemoryDump.Alloc((uint)b_MyFile.Length);
            SprMemoryDump.Write_Bytes(b_MyFile);

            //mov eax, Size of new file instead of seeking the original
            WriteByte((UInt32)_Process_MemoryBaseAddress + _OverrideSprLogoSize_Offset, 0xB8);
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _OverrideSprLogoSize_Offset + 1, BitConverter.GetBytes((UInt32)(b_MyFile.Length)));

            //Replacing the fread function call, instead we fill the memory directly
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            Buffer = new List<byte>();
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi, ZipMemoryAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(SprMemoryDump.CaveAddress));
            //mov edi, ebx
            CaveMemory.Write_StrBytes("8B FB");
            //mov ecx, ZipMemoryAddress.Size
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)b_MyFile.Length));
            //rep movsb
            CaveMemory.Write_StrBytes("F3 A4");
            //pop edi
            CaveMemory.Write_StrBytes("59");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop ecx
            CaveMemory.Write_StrBytes("5F");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _OverrideSprLogo_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_InjectSprTitleExtras() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _OverrideSprLogo_Injection.Injection_Offset) - 5;
            Buffer.Clear();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _OverrideSprLogo_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //Replace the original spr_menuextras.zip by the one we have in memory, at loading time
        //First step is to change the malloc size called to read the file
        //Second step is to copy our own data in the malloc space reserved
        //Menu is skipped so we can use these sprites without any risks
        //To acces the sprites, use following EDI values when calling Sub_4A5AC0() :
        //599DB4 = crosshair_e.dds
        //599DC0 = crosshair_g.dds
        //599DCC = crosshair_f.dds
        //599DD8 = crosshair_s.dds
        //599DE4 = crosshair_i.dds
        //599DF0 = crosshair_j.dds
        //599DFC = end.dds
        //etc...
        private void Mod_InjectSprMenuExtras()
        {
            byte[] b_MyFile = Properties.Resources.spr_menuextras_en;

            //OR we can check for language setting and load the corresponding spr_menuextra from Resource file.
            //BUT this will cause issues with sprites spacing as each language has different size
            switch(_GameConfigurator.Language)
            {
                case 0: b_MyFile = Properties.Resources.spr_menuextras_en; break;
                case 1: b_MyFile = Properties.Resources.spr_menuextras_fr; break;
                case 2: b_MyFile = Properties.Resources.spr_menuextras_it; break;
                case 3: b_MyFile = Properties.Resources.spr_menuextras_es; break;
                default: b_MyFile = Properties.Resources.spr_menuextras_en; break;
            }

            //Store zip file in game memory 
            Codecave SprMemoryDump = new Codecave(_Process, _Process.MainModule.BaseAddress);
            SprMemoryDump.Open();
            SprMemoryDump.Alloc((uint)b_MyFile.Length);
            SprMemoryDump.Write_Bytes(b_MyFile);

            //mov eax, Size of new file instead of seeking the original
            WriteByte((UInt32)_Process_MemoryBaseAddress + _OverrideSprMenuExtrasSize_Offset, 0xB8);
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _OverrideSprMenuExtrasSize_Offset + 1, BitConverter.GetBytes((UInt32)b_MyFile.Length));

            //Replacing the fread function call, instead we fill the memory directly
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            Buffer = new List<byte>();
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi, ZipMemoryAddress
            CaveMemory.Write_StrBytes("BE");
            Buffer.AddRange(BitConverter.GetBytes(SprMemoryDump.CaveAddress));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //mov edi, ebx
            CaveMemory.Write_StrBytes("8B FB");
            //mov ecx, ZipMemoryAddress.Size
            CaveMemory.Write_StrBytes("B9");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)b_MyFile.Length));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //rep movsb
            CaveMemory.Write_StrBytes("F3 A4");
            //pop edi
            CaveMemory.Write_StrBytes("59");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop ecx
            CaveMemory.Write_StrBytes("5F");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _OverrideSprMenuExtras_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_InjectSprMenuExtras() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _OverrideSprMenuExtras_Injection.Injection_Offset) - 5;
            Buffer.Clear();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _OverrideSprMenuExtras_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }
               
        //Draw PRESS START BUTTON sprite (PRESS START or INSERT COIN(S))
        //X and Y values passed by Stack by previous calling functions   
        private void Mod_CreateDrawSprite1_Function(float fScaleX, float fScaleY, byte bTransform)
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer = new List<byte>();            
            //cmp [Databank_CustomSpriteVisibility], 0
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomSpriteVisibility));
            CaveMemory.Write_StrBytes("00");
            //je Exit
            CaveMemory.Write_StrBytes("74 4E");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //cmp [FREEPLAY], 1
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FlagFreeplay_Offset));
            CaveMemory.Write_StrBytes("01");
            //je PRESSSTART
            CaveMemory.Write_StrBytes("74 14");
            //mov al, [CreditsToStart]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CreditsToStart_Offset));
            //cmp [Credits], eax
            CaveMemory.Write_StrBytes("39 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            //jge PRESSSTART
            CaveMemory.Write_StrBytes("7D 07");
            //INSERTCOIN :
            //mov edi, ID_SPRITE_INSERTCOIN
            CaveMemory.Write_StrBytes("BF");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.InsertCoin));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp draw_sprite
            CaveMemory.Write_StrBytes("EB 05");
            //PRESS START:
            //mov edi, ID_SPRITE_PRESSSTART
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.PressStart));
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push Transform
            CaveMemory.Write_StrBytes("6A");
            CaveMemory.Write_Bytes(new byte[] { bTransform });
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push ScaleY
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleX));
            //push ScaleX
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleY));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push [esp+2C] = Y
            CaveMemory.Write_StrBytes("FF 74 24 2C");
            //push [ESP+2C] = X
            CaveMemory.Write_StrBytes("FF 74 24 2C");
            //call 4A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _CustomDrawSprite1_Function_Address = CaveMemory.CaveAddress;
            Logger.WriteLog("Mod_TitleScreen() => Mod_CreateDrawSprite1_Function at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        //Draw CREDITS sprite or FREEPLAY
        //X and Y values passed by Stack by previous calling functions               
        private void Mod_CreateDrawSprite2_Function(float fScaleX, float fScaleY, byte bTransform)
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer = new List<byte>();            
            //push edi
            CaveMemory.Write_StrBytes("57");
            //cmp [FREEPLAY], 0
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FlagFreeplay_Offset));
            CaveMemory.Write_StrBytes("00");
            //je CREDITS
            CaveMemory.Write_StrBytes("74 0A");
            //FREEPLAY:
            //mov edi, ID_SPRITE_FREEPLAY
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Freeplay));
            //jmp draw_freeplay
            CaveMemory.Write_StrBytes("E9 92 00 00 00");
            
            //CREDITS:
            //mov edi, CRREDITS
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Credits));
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push Transform
            CaveMemory.Write_StrBytes("6A");
            CaveMemory.Write_Bytes(new byte[] { bTransform });
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push ScaleY
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleX));
            //push ScaleX
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleY));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push [esp+30] = Y
            CaveMemory.Write_StrBytes("FF 74 24 30");
            //push [ESP+30] = X
            CaveMemory.Write_StrBytes("FF 74 24 30");
            //fld dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //fsub dword ptr [flt_16.0f]
            CaveMemory.Write_StrBytes("D8 25");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FltConstant_16f_Offset));
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");

            //call 4A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);

            //DIGIT 0:
            //fld dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //fadd dword ptr [flt_46.0f]
            CaveMemory.Write_StrBytes("D8 05");
            if (_GameConfigurator.Language == 3)
                CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FltConstant_58f_Offset));
            else
                CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FltConstant_46f_Offset));
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");
            //mov eax,[CREDITS]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            //mov ebx,0000000A
            CaveMemory.Write_StrBytes("BB 0A 00 00 00");
            //div bl
            CaveMemory.Write_StrBytes("F6 F3");
            //movzx eax, al
            CaveMemory.Write_StrBytes("0F B6 C0");
            //mov bl,0C
            CaveMemory.Write_StrBytes("B3 0C"); 
            //mul bl
            CaveMemory.Write_StrBytes("F6 E3"); 
            //movzx edi,al
            CaveMemory.Write_StrBytes("0F B6 F8");
            //add edi, SPRITES_ADD
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Num0));
            //call 4A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            
            //DIGIT 1
            //fld dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //fadd dword ptr [flt_10.0f]
            CaveMemory.Write_StrBytes("D8 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FltConstant_10f_Offset));
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");
            //mov eax,[CREDITS]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            //mov ebx,0000000A
            CaveMemory.Write_StrBytes("BB 0A 00 00 00");
            //div bl
            CaveMemory.Write_StrBytes("F6 F3");
            //shr eax, 8
            CaveMemory.Write_StrBytes("C1 E8 08");
            //mov bl,0C
            CaveMemory.Write_StrBytes("B3 0C");
            //mul bl
            CaveMemory.Write_StrBytes("F6 E3");
            //movzx edi,al
            CaveMemory.Write_StrBytes("0F B6 F8");
            //add edi, SPRITES_ADD
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Num0));
            //call 4A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 20");

            //Draw Freeplay :
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push Transform
            CaveMemory.Write_StrBytes("6A");
            CaveMemory.Write_Bytes(new byte[] { bTransform });
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push ScaleY
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleX));
            //push ScaleX
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleY));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push [esp+30] = Y
            CaveMemory.Write_StrBytes("FF 74 24 30");
            //push [ESP+30] = X
            CaveMemory.Write_StrBytes("FF 74 24 30");
            //call 4A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _CustomDrawSprite2_Function_Address = CaveMemory.CaveAddress;
            Logger.WriteLog("Mod_TitleScreen() => Mod_CreateDrawSprite2_Function at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        //Call both sprites functions to draw the whole thing
        //X and Y coordinates are in the stack as arguments
        private void Mod_CreateDrawSpritesAll_Function()
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //Call CustomDrawSpriteFunction_1            
            CaveMemory.Write_call(_CustomDrawSprite1_Function_Address);
            //Call CustomDrawSpriteFunction_2
            CaveMemory.Write_call(_CustomDrawSprite2_Function_Address);
            //ret
            CaveMemory.Write_StrBytes("C3");

            _CustomDrawSpriteAll_Function_Address = CaveMemory.CaveAddress;
            Logger.WriteLog("Mod_AddScreenSprites() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        //Add CREDITS and replace the original PRESS START BUTTON sprite by new ones (PRESS START or INSERT COIN(S))  
        //Also check for Intro Movie to be skipped on 1st time to get to Title Screen directly (send 1 to the byte written on a [ENTER] press
        //And skip 1st attract mode to show unseen intro movie instead (change screen ID)
        //And skip Menu (sending [ENTER])
         private void Mod_AddLotOfThingsIntoQuickThread()
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
             //First : save registers...lots of functions will be called, and modify them
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push edx
            CaveMemory.Write_StrBytes("52");

            //First : Reset display RELOAD srpites if reloaded
            //cmp dword ptr [P1_Ammo], 00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Player1Ammo_Offset));
            CaveMemory.Write_StrBytes("00");
            //jle CheckP2
            CaveMemory.Write_StrBytes("7E 0A");
            //mov [DataBank.DisplayP1Reload], 0
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.ShowReloadP1));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //cmp dword ptr [P2_Ammo], 00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Player2Ammo_Offset));
            CaveMemory.Write_StrBytes("00");
            //jle Next
            CaveMemory.Write_StrBytes("7E 0A");
            //mov [DataBank.DisplayP2Reload], 0
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.ShowReloadP2));
            CaveMemory.Write_StrBytes("00 00 00 00");

            //Second : Check to display RELOAD srpites
            //cmp dword ptr [DataBank_Offset.ShowReloadP1], 00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.ShowReloadP1));
            CaveMemory.Write_StrBytes("00");
            //jne CheckP2
            CaveMemory.Write_StrBytes("74 38");
            //cmp dword ptr [P1_GameStatus], 05
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Player1GameStatus_Offset));
            CaveMemory.Write_StrBytes("05");
            //jne CheckP2
            CaveMemory.Write_StrBytes("75 2F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0x05
            CaveMemory.Write_StrBytes("6A 05");
            //push 0x00
            CaveMemory.Write_StrBytes("6A 00");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(240.0f));
            //push X
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(70.0f));
            //push 0x90 (Reload sprite ID)
            CaveMemory.Write_StrBytes("68 90 00 00 00");
            //Call draw
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFsSpritesFunction_Offset);
            //add esp, 24
            CaveMemory.Write_StrBytes("83 C4 24");
            //CheckP2
            //cmp dword ptr [DataBank_Offset.ShowReloadP2],01
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.ShowReloadP2));
            CaveMemory.Write_StrBytes("00");
            //je Next
            CaveMemory.Write_StrBytes("74 38");
            //cmp dword ptr [P2_GameStatus], 05
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Player2GameStatus_Offset));
            CaveMemory.Write_StrBytes("05");
            //jne CheckP2
            CaveMemory.Write_StrBytes("75 2F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0x07
            CaveMemory.Write_StrBytes("6A 07");
            //push 0x00
            CaveMemory.Write_StrBytes("6A 00");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(240.0f));
            //push X
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(570.0f));
            //push 0x90 (Reload sprite ID)
            CaveMemory.Write_StrBytes("68 90 00 00 00");
            //Call draw
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFsSpritesFunction_Offset);
            //add esp, 24
            CaveMemory.Write_StrBytes("83 C4 24");

            //Second Step : 
            //cmp dword ptr [Scene_ID],04
            CaveMemory.Write_StrBytes("83 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("04");
            //jne Next
            CaveMemory.Write_StrBytes("75 1D");
            //cmp dword ptr [SkipIntro],01
            CaveMemory.Write_StrBytes("83 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.SkipIntro));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("01");
            //jne Next
            CaveMemory.Write_StrBytes("75 14");
            //mov [hod3pc.exe+3ECF48],00000001
            CaveMemory.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _EnterKeyPressed_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("01 00 00 00");
            //mov [SkipIntro],00000000
            CaveMemory.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.SkipIntro));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("00 00 00 00");

            //cmp dword ptr [Scene_ID],06
            CaveMemory.Write_StrBytes("83 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("06");
            //jne Next
            CaveMemory.Write_StrBytes("75 1D");            
            //cmp dword ptr [ShowIntro],01
            CaveMemory.Write_StrBytes("83 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.ShowIntro));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("01");
            //jne Next
            CaveMemory.Write_StrBytes("75 14");  
            //mov [NextScene_ID],00000004
            CaveMemory.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _NextSceneID_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("04 00 00 00");
            //mov [ShowIntro],00000000
            CaveMemory.Write_StrBytes("C7 05");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.ShowIntro));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("00 00 00 00");

            //call dword ptr [hod3pc.exe+1491C0] GetTickCount()
            CaveMemory.Write_StrBytes("FF 15 C0 91 54 00");
            //cmp dword ptr [Databank_BlinkTinkCount],00
            CaveMemory.Write_StrBytes("83 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.BlinkTickCount));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("00");
            //jne CheckBlinkDelay
            CaveMemory.Write_StrBytes("75 0C");
            //mov [Databank_BlinkTinkCount], eax
            CaveMemory.Write_StrBytes("A3");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.BlinkTickCount));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //mov [Databank_FrameTickCount], eax
            CaveMemory.Write_StrBytes("A3");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.FrameTickCount));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp DrawSprites
            CaveMemory.Write_StrBytes("EB 3A");
            //CheckBlinkDelay:
            //mov ebx, eax
            CaveMemory.Write_StrBytes("8B D8");
            //sub ebx, [Databank_BlinkTinkCount]
            CaveMemory.Write_StrBytes("2B 1D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.BlinkTickCount));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //cmp ebx,00000300
            CaveMemory.Write_StrBytes("81 FB 00 03 00 00");
            //jle CheckFrameDelay
            CaveMemory.Write_StrBytes("7E 18");
            //SwitchVisibility:
            //mov [Databank_BlinkTinkCount],eax
            CaveMemory.Write_StrBytes("A3");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.BlinkTickCount));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //mov ebx, [Databank_SpriteVisibility]
            CaveMemory.Write_StrBytes("8B 1D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomSpriteVisibility));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            // mov ecx,00000001
            CaveMemory.Write_StrBytes("B9 01 00 00 00");
            //sub ecx,ebx
            CaveMemory.Write_StrBytes("29 D9");
            //mov [Databank_SpriteVisibility], ecx
            CaveMemory.Write_StrBytes("89 0D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.CustomSpriteVisibility));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //CheckFrameDelay:
            //mov ebx,eax
            CaveMemory.Write_StrBytes("8B D8");
            //sub ebx, [Databank_FrameTinkCount]
            CaveMemory.Write_StrBytes("2B 1D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.FrameTickCount));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //cmp ebx,10
            CaveMemory.Write_StrBytes("83 FB 01");
            //jl Exit
            CaveMemory.Write_StrBytes("7C 35");
            //ShowSprite:
            //mov [Databank_BlinkTinkCount],eax
            CaveMemory.Write_StrBytes("A3");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.FrameTickCount));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //mov eax,  [SceneID]
            CaveMemory.Write_StrBytes("A1");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //cmp eax, 2
            CaveMemory.Write_StrBytes("83 F8 02");
            //je exit
            CaveMemory.Write_StrBytes("74 26");
            //cmp eax, 10
            CaveMemory.Write_StrBytes("83 F8 0A");
            //je exit
            CaveMemory.Write_StrBytes("74 21");
            //cmp eax, 11
            CaveMemory.Write_StrBytes("83 F8 0B");
            //je exit
            CaveMemory.Write_StrBytes("74 1C");
            //push Y2 = 450.0f
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X2 = 320.0f
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(320.0f));
            //push Y2 = 431.0f
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_UpperSpriteInfo_Y));
            //push X2 = 320.0f
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(320.0f));
            //Call CustomDrawSpriteFunction_1
            CaveMemory.Write_call(_CustomDrawSpriteAll_Function_Address);
            //Call CustomDrawSpriteFunction_1
            //add esp, 10
            CaveMemory.Write_StrBytes("83 C4 10");

            //Resore registers before returning
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov eax, [esp+14]
            CaveMemory.Write_StrBytes("8B 44 24 14");
            //mov ecx, [eax]
            CaveMemory.Write_StrBytes("8B 08");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _LoopThreadVariousThings_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_AddScreenSprites() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _LoopThreadVariousThings_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _LoopThreadVariousThings_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }
        
        //Changing the flow of intro logo at boot, more like arcade
        private void Mod_IntroductionLoop()
        {
            WriteByte((UInt32)_Process_MemoryBaseAddress + _IntroLogoChange_Offset1, 0x01);
            WriteByte((UInt32)_Process_MemoryBaseAddress + _IntroLogoChange_Offset2, 0x01);
            WriteByte((UInt32)_Process_MemoryBaseAddress + _IntroLogoChange_Offset3, 0x07);
        }

        //Making the Title Screen a little more "arcade"
        private void Mod_TitleScreen()
        {
            //Removing the sound when validating. The sound from the automated next menu will play, and we can use this sound for credits instead
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_SfxPlayStart);  

            ///Removing old Title (alpha=0)
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _ReplaceTitleLogo_Offset, new byte[] { 0x90, 0x90, 0x90, 0x57, 0x6A, 0x00 });  //pushing alpha=0 instead of fading EAX value

            //Remove old PRESS START
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _ReplaceTitleLogo_Offset + 0xB7, new byte[] { 0x31, 0xD2, 0x90, 0x90 });
            
            //New Logo
            //pushing fading alpha in ESP
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _ReplaceTitleLogo_Offset + 0x2F, new byte[] { 0x90, 0xFF, 0x74, 0x24, 0x2C });
            WriteByte((UInt32)_Process_MemoryBaseAddress + _ReplaceTitleLogo_Offset + 0x35, 0x0A);  //Centered
            
            //Method 2 : Keep Old Trademark (change location) and add a new sprite
            //Location of old trademark
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _ReplaceTitleLogo_Offset + 0x48, BitConverter.GetBytes(390.0f));
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _ReplaceTitleLogo_Offset + 0x4D, BitConverter.GetBytes(320.0f));
            //ID
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _ReplaceTitleLogo_Offset + 0x54, BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset +(uint)SprMenuExtraSprite.Trademark));
            //New Sprite for Logo
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //call 04A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //mov [esp],00000000
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(320.0f));
            //mov [esp-04],00000000
            CaveMemory.Write_StrBytes("C7 44 24 04");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(240.0f));
            //mov edi,hod3pc.exe+199D54
            CaveMemory.Write_StrBytes("BF");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Logo));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //call 04A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ChangeTitleLogo_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_TitleScreen() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ChangeTitleLogo_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ChangeTitleLogo_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);            
        }

        //Replaceing instructions sprites with arcade ones (no more autoreload, etc...)
        private void Mod_ReplaceAttractModeSprites()
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //cmp [esp], C4
            CaveMemory.Write_StrBytes("81 3C 24 C4 00 00 00");
            //jne Check2
            CaveMemory.Write_StrBytes("75 3F");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov edi, RELOAD_SPRITE_ID
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.AttractReload));
            //mov eax, [esp+2C]
            CaveMemory.Write_StrBytes("8B 44 24 2C");   
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push 05
            CaveMemory.Write_StrBytes("6A 05");
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push 3f800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 3f800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 37a7c5ac
            CaveMemory.Write_StrBytes("68 AC C5 A7 37");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(160.0f));
            //push X
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(96.0f));
            //call 4A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //mov [esp+20], 0
            CaveMemory.Write_StrBytes("C7 44 24 20 00 00 00 00");
            //jmp return
            CaveMemory.Write_StrBytes("EB 62");

            //Check 2:
            //cmp [esp], BF
            CaveMemory.Write_StrBytes("81 3C 24 BF 00 00 00");
            //jne Check3
            CaveMemory.Write_StrBytes("75 3F");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov edi, RELOAD_SPRITE_ID
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.AttractShells));
            //mov eax, [esp+2C]
            CaveMemory.Write_StrBytes("8B 44 24 2C");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push 05
            CaveMemory.Write_StrBytes("6A 05");
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push 3f800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 3f800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 37a7c5ac
            CaveMemory.Write_StrBytes("68 AC C5 A7 37");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(284.0f));
            //push X
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(96.0f));
            //call 4A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //mov [esp+20], 0
            CaveMemory.Write_StrBytes("C7 44 24 20 00 00 00 00");
            //jmp return
            CaveMemory.Write_StrBytes("EB 1A");

            //Check3:
            //cmp [esp], CB
            CaveMemory.Write_StrBytes("81 3C 24 CB 00 00 00");
            //je remove
            CaveMemory.Write_StrBytes("74 09");
            //cmp [esp], C6
            CaveMemory.Write_StrBytes("81 3C 24 C6 00 00 00");
            //jne return
            CaveMemory.Write_StrBytes("75 08");
            //remove:
            //mov [esp+20], 0
            CaveMemory.Write_StrBytes("C7 44 24 20 00 00 00 00");
            //return:
            //call hod3pc.exe+A5EA0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFsSpritesFunction_Offset);
            //jmp returnhere
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ReplaceAttractModeSprites_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_ReplaceAttractModeSprite() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ReplaceAttractModeSprites_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ReplaceAttractModeSprites_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);    
        }

        //The game is alterning between "arcade" attract mode and "time attack" attract mode by switching a byte 0/1
        //We will just force the jump whatever the value is to only keep Arcade
        //Same thing for Ranking, remove Time Attack display by forcing the read value to 0
        private void Mod_DisableTimeAttackAttractMode()
        {
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _DisableTimeAttackAttractMode_Offset, new byte[] { 0xEB, 0x23 });
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _DisableTimeAttackRankingDisplay_Offset, new byte[] { 0x31, 0xC0, 0x90 });
        }

        //By Default the game set Credits to "0" and init credits at each new game with the right number based on CreditsToStart
        //We will override this so that we are the only ones to add them and no reset at the end of the game
        private void Mod_CreditsHandling()
        {
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_CreditsInit_1);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_CreditsInit_2);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_CreditsInit_3);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_CreditsInit_4);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_CreditsToStartAndContinueInit);
            WriteByte((UInt32)_Process_MemoryBaseAddress + _CreditsToStart_Offset, (byte)_GameConfigurator.CreditsToStart);
            WriteByte((UInt32)_Process_MemoryBaseAddress + _CreditsToContinue_Offset, (byte)_GameConfigurator.CreditsToContinue);
        }

        //Disable Start P1 and Start P2 if no credits at the title screen
        //Also check for FREEPLAY to enable
        private void Mod_BlockTitleIfNoCredits()
        {/*
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //je exit
            //Button Pushed:
            CaveMemory.Write_StrBytes("74 25");
            //cmp [SceneID], 5
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("05");
            //jne ok
            CaveMemory.Write_StrBytes("75 19");
            //cmp [Frepplay], 1
            CaveMemory.Write_StrBytes("83 3D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FlagFreeplay_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("01");
            //je ok
            CaveMemory.Write_StrBytes("74 10");
            //xor ebx, ebx
            CaveMemory.Write_StrBytes("31 DB");
            //mov bl, [CreditsToStart]
            CaveMemory.Write_StrBytes("8A 1D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CreditsToStart_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //cmp [Credits],ebx
            CaveMemory.Write_StrBytes("39 1D");
            Buffer = new List<byte>();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jge exit
            CaveMemory.Write_StrBytes("7C 03");
            //ok:
            //or byte ptr [ecx],10
            CaveMemory.Write_StrBytes("80 09 10");
            //exit:
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _BlockTitleStartIfNoCredits_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_BlockTitleIfNoCredits() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _BlockTitleStartIfNoCredits_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _BlockTitleStartIfNoCredits_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        */

            _BlockTitleStartIfNoCredits_Injection = new InjectionStruct(0x8E000, 5);
            NopStruct n = new NopStruct(0x8E015, 7);
            SetNops((UInt32)_Process_MemoryBaseAddress, n);

            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //cmp [SceneID], 5
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("05");
            //jne ok
            CaveMemory.Write_StrBytes("75 1B");
            //cmp [Frepplay], 1
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FlagFreeplay_Offset));
            CaveMemory.Write_StrBytes("01");
            //je ok
            CaveMemory.Write_StrBytes("74 12");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //xor ebx, ebx
            CaveMemory.Write_StrBytes("31 DB");
            //mov bl, [CreditsToStart]
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CreditsToStart_Offset));
            //cmp [Credits],ebx
            CaveMemory.Write_StrBytes("39 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //jge exit
            CaveMemory.Write_StrBytes("7C 0C");
            //ok:
            //call 432C10
            CaveMemory.Write_call(0x432C10);
            //mov 7B8964, esi
            CaveMemory.Write_StrBytes("66 89 35 64 89 7B 00");
            //exit:
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _BlockTitleStartIfNoCredits_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_BlockTitleIfNoCredits() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _BlockTitleStartIfNoCredits_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _BlockTitleStartIfNoCredits_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
       
        
        }

        // Add the Title SCENE_ID in allowed ID to start a game, instead of the MENU_ID
        private void Mod_StartGameFromTitleScreen()
        {
            //Enable the code to run when in Title screen instead of Menu screen
            WriteByte((UInt32)_Process_MemoryBaseAddress +  _StartGameFromTitle_Offset1, 0x05);
            WriteByte((UInt32)_Process_MemoryBaseAddress +  _StartGameFromTitle_Offset2, 0x05);            

            //Adding possibility to go into the good prrocedure, when SCENE_ID is Title, in a switch loop
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //cmp edi,05
            CaveMemory.Write_StrBytes("83 FF 05");
            //jne 00B10018
            CaveMemory.Write_StrBytes("75 0F");
            //mov [hod3pc.exe+366748],00000001
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _AllowStartGame));
            CaveMemory.Write_StrBytes("01 00 00 00");
            //jmp Case"5"
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _StartGameFromTitleSwitchCase_Offset);            
            //movzx eax,byte ptr [eax+hod3pc.exe+8E134]
            CaveMemory.Write_StrBytes("0F B6 80");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _StartGameFromByteJmpTable_Offset));
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _StartGameFromTitle_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_BlockTitleIfNoCredits() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _StartGameFromTitle_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _StartGameFromTitle_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten); 
        }


        //Remove mouse Cursor Everywhere
        private void Mod_HideCursor()
        {
            WriteByte((UInt32)_Process_MemoryBaseAddress + 0x32BDF, 0x00);
            WriteByte((UInt32)_Process_MemoryBaseAddress + 0x32B7E, 0x00);
        }

        //Shunting the menu, displayed as a short black screen before running the game directly
        private void Mod_RemoveMenu_Screen()
        {
            /*** Nopping the Sprites drawing for the Menu : background, logo, item and selection arrows ***/
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_MenuScreen_Background);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_MenuScreen_Logo);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_MenuScreen_Copyright);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_MenuScreen_ItemText);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_MenuScreen_Arrow1);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_MenuScreen_Arrow2);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_MenuScreen_Arrow3);
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_MenuScreen_Arrow4);

            /*** Removing the mouse cursor on the screen ***/
            /*** This hack also removes cursor from game pause popup :'( ***/
            /*Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //cmp esi, 2
            CaveMemory.Write_StrBytes("83 FE 02");
            //jne originalcode
            CaveMemory.Write_StrBytes("75 0B");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //mov byte ptr [hod3pc.exe+21217A],00
            CaveMemory.Write_StrBytes("C6 05 7A 21 61 00 00");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 09");
            //originalcode : push 01
            CaveMemory.Write_StrBytes("6A 01");
            //mov byte ptr [hod3pc.exe+21217A],01
            CaveMemory.Write_StrBytes("C6 05 7A 21 61 00 01");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _MenuScreenCursor_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_RemoveMenu_Screen() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _MenuScreenCursor_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _MenuScreenCursor_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
            */

            //Other ShowCursor() call @432B70 sub !!
            //Called on in-game pause/quit poppup

            /*** Autoselecting the MenuItem on entering => the menu will act as a quick-displayed black screen between the main title and the start of the game ***/
            WriteByte((UInt32)_Process_MemoryBaseAddress + _MainMenuAutoValidate_Offset, 0xEB);
        }

        //On the game, pausing then returning to the main menu will be redirected to the Title Screen (Main menu does not exists)
        //For that, when the game asks for scene ID 2 (menu) from scene ID 10 (game) > redirect to scene ID 5 (title)
        private void Mod_ReturnToMenu()
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //cmp esi, 2
            CaveMemory.Write_StrBytes("83 FE 02");
            //jne originalcode
            CaveMemory.Write_StrBytes("75 0E");
            //cmp [CURRENT_SCENE_ID],0000000A
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("0A");
            //jne originalcode
            CaveMemory.Write_StrBytes("75 05");
            //mov esi,00000005
            CaveMemory.Write_StrBytes("BE 05 00 00 00");
            //originalcode : cmp esi, 8
            CaveMemory.Write_StrBytes("83 FE 08");
            //jl @offset_32BD5
            CaveMemory.Write_jl((UInt32)_Process_MemoryBaseAddress + _ReturnToMenu_Injection.Injection_Offset + 0x25);
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ReturnToMenu_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_RemoveMenu_Screen() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ReturnToMenu_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ReturnToMenu_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //By default the game is limiting Max_Life to 9, whereas in arcade you can choose from 5 to 9
        //And we need to limit the game to max life number when it is initializing the game, as start life can be greater than max
        private void Mod_SetMaxLife()
        {
            WriteByte((UInt32)_Process_MemoryBaseAddress + _MaxLife_Offset, (byte)_GameConfigurator.MaxLife);

            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //movsx eax,byte ptr [edx+00000186]
            CaveMemory.Write_StrBytes("0F BE 82 86 01 00 00");
            //cmp eax, _Option_MaxLife
            CaveMemory.Write_StrBytes("83 F8");
            CaveMemory.Write_Bytes(new byte[] { (byte)(_GameConfigurator.MaxLife - 1) });    //Game is using a table indexed from 0 to load life number
            //jle
            CaveMemory.Write_StrBytes("7E 05");
            //mov eax, _Option_MaxLife
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(new byte[] { (byte)(_GameConfigurator.MaxLife - 1), 0x00, 0x00, 0x00 });
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _InitLifeLimitToMax_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_SetMaxLife() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _InitLifeLimitToMax_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _InitLifeLimitToMax_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //Disable the Pause menu when hitting Start in-game
        private void Mod_DisableInGamePause_v1()
        {
            //Solution 1 : force jmp if pause is detected
            WriteByte((UInt32)_Process_MemoryBaseAddress + _DisableInGamePause_v1_Offset, 0xEB);
        }
        private void Mod_DisableInGamePause_v2()
        {
            //Other possibility : xor eax, eax @48C2B3 
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _DisableInGamePause_v2_Offset, new byte[] { 0x31, 0xC0, 0x90, 0x90 });
        }

         //Hide gun display during gameplay, like arcade version of the game
        //Only display them during reload
        private void Mod_HideGuns()
        {
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_Arcade_Mode_Display);
        }

        //Fastening the reload animation so that it's more like arcade version of the game
        //Otherwise rthe reload animation is toooooo long
        private void Mod_FastReload()
        {
            //If no auto-reload, we also shrink the reload animation time to make it faster, like in arcade
            WriteByte((UInt32)_Process_MemoryBaseAddress + _FastReload_Offset + 0x04, 0x11); //Reload Animation duration, in Frames.
            WriteByte((UInt32)_Process_MemoryBaseAddress + _FastReload_Offset + 0x1C, 0x11); //Reload Animation duration, in Frames.
            WriteByte((UInt32)_Process_MemoryBaseAddress + _FastReload_Offset + 0x18, 0x09); //Gun (?) Animation duration, in Frames.
            WriteByte((UInt32)_Process_MemoryBaseAddress + _FastReload_Offset, 0x09); //Hand (?) Animation duration, in Frames.
        }

        //Disable auto-reload added on the PC game
        private void Mod_NoAutoReload()
        {
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_NoAutoReload_1);
        }

        //Enable the RELOAD text to appear, and RELOAD Sfx to play when it's needed
        //SFX drawing will be done on the "VariousThread Mod" because overwise, sprites are poors without transparency :(
        private void Mod_AddReloadEffects()
        {
            //Adding the Sfx when trigger is pushed with no bullets.
            Codecave CaveMemory_Reload1 = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory_Reload1.Open();
            CaveMemory_Reload1.Alloc(0x800);
            List<byte> Buffer;
            //push eax
            CaveMemory_Reload1.Write_StrBytes("50");
            //push ecx
            CaveMemory_Reload1.Write_StrBytes("51");
            //mov eax, [eax+4]
            CaveMemory_Reload1.Write_StrBytes("8B 40 04");
            //shl eax, 2
            CaveMemory_Reload1.Write_StrBytes("C1 E0 02");
            //add eax, CustomDatabank.DisllayReload
            CaveMemory_Reload1.Write_StrBytes("05");
            CaveMemory_Reload1.Write_Bytes(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.ShowReloadP1));
            //mov [eax, 1]
            CaveMemory_Reload1.Write_StrBytes("C7 00 01 00 00 00");            
            //push 00
            CaveMemory_Reload1.Write_StrBytes("6A 00");
            //push REloadSfx_ID
            CaveMemory_Reload1.Write_StrBytes("68");
            CaveMemory_Reload1.Write_Bytes(BitConverter.GetBytes(_SfxID_Reload));
            //push 9D2E18
            CaveMemory_Reload1.Write_StrBytes("68");
            CaveMemory_Reload1.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SoundPlayerHandle_Offset));
            //call play audio
            CaveMemory_Reload1.Write_call((UInt32)_Process_MemoryBaseAddress + _WndProcCredits_CalledFunctionOffset);
            //pop ecx
            CaveMemory_Reload1.Write_StrBytes("59");
            //pop eax
            CaveMemory_Reload1.Write_StrBytes("58");
            //jmp return
            CaveMemory_Reload1.Write_jmp((UInt32)_Process_MemoryBaseAddress + _AddReloadSfx_Injection1.Injection_ReturnOffset);

            Logger.WriteLog("Mod_AddReloadEffects() => Adding codecave at " + CaveMemory_Reload1.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory_Reload1.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _AddReloadSfx_Injection1.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _AddReloadSfx_Injection1.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            //-----------------------------------//

            //Adding the Sfx when bullet count hits 0.
            Codecave CaveMemory_Reload2 = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory_Reload2.Open();
            CaveMemory_Reload2.Alloc(0x800);
            //push eax
            CaveMemory_Reload2.Write_StrBytes("50");
            //push ecx
            CaveMemory_Reload2.Write_StrBytes("51");
            //mov eax, [eax+4]
            CaveMemory_Reload2.Write_StrBytes("8B 40 04");
            //shl eax, 2
            CaveMemory_Reload2.Write_StrBytes("C1 E0 02");
            //add eax, CustomDatabank.DisllayReload
            CaveMemory_Reload2.Write_StrBytes("05");
            CaveMemory_Reload2.Write_Bytes(BitConverter.GetBytes(_DataBank_Address + (uint)DataBank_Offset.ShowReloadP1));
            //mov [eax, 1]
            CaveMemory_Reload2.Write_StrBytes("C7 00 01 00 00 00");  
            //push 00
            CaveMemory_Reload2.Write_StrBytes("6A 00");
            //push REloadSfx_ID
            CaveMemory_Reload2.Write_StrBytes("68");
            CaveMemory_Reload2.Write_Bytes(BitConverter.GetBytes(_SfxID_Reload));
            //push 9D2E18
            CaveMemory_Reload2.Write_StrBytes("68");
            CaveMemory_Reload2.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SoundPlayerHandle_Offset));
            //call play audio
            CaveMemory_Reload2.Write_call((UInt32)_Process_MemoryBaseAddress + _WndProcCredits_CalledFunctionOffset);
            //pop ecx
            CaveMemory_Reload2.Write_StrBytes("59");
            //pop eax
            CaveMemory_Reload2.Write_StrBytes("58");
            //jmp return
            CaveMemory_Reload2.Write_jmp((UInt32)_Process_MemoryBaseAddress + _AddReloadSfx_Injection2.Injection_ReturnOffset);

            Logger.WriteLog("Mod_AddReloadEffects() => Adding codecave at " + CaveMemory_Reload2.CaveAddress.ToString("X8"));

            //Code injection
            ProcessHandle = _Process.Handle;
            bytesWritten = 0;
            jumpTo = 0;
            jumpTo = CaveMemory_Reload2.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _AddReloadSfx_Injection2.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _AddReloadSfx_Injection2.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        
        }
        
        //Replace old and ugly "FREEPLAY" displayed on lower screen during gameplay
        private void Mod_ReplaceFreeplaySpriteInGame()
        {
            //Adding the new one
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X));
            //cmp edi,01
            CaveMemory.Write_StrBytes("83 FF 01");
            //mov edi, [FREEPLAY SPRITE ID]
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Freeplay));
            //jne Next
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X));
            //Next:            
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 00000000
            CaveMemory.Write_StrBytes("68 00 00 00 00");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ReplaceFreeplaySpriteInGame_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_ReplaceFreeplaySpriteInGame() => Adding codecave at " + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ReplaceFreeplaySpriteInGame_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ReplaceFreeplaySpriteInGame_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }
        
        //Same thing for the CREDITS sprite
        private void Mod_ReplaceCreditsSpriteInGame()
        {
            //Adding the new one
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX1));
            //cmp edi,01
            CaveMemory.Write_StrBytes("83 FF 01");
            //mov edi, [CREDITS SPRITE ID]
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Credits));
            //jne Next
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX1));
            //Next:            
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 00000000
            CaveMemory.Write_StrBytes("68 00 00 00 00");            
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsSpriteInGame_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_ReplaceCreditsSpriteInGame() => Adding codecave at " + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsSpriteInGame_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsSpriteInGame_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //Original Sprite ID is [0x40 - 0x49]
        //Has to be converted for my table in the range [0-9]
        //During cutscenes, a bug make them diseappear behind bottom black borders.....
        //Only solution is to know how to override genuine spritesfrom SPR.AFS
        private void Mod_ReplaceCreditsDigit1SpriteInGame()
        {
            //Forcing the display of the 2 digits
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsDigit1SpriteInGame_Injection.Injection_Offset - 0x06, new byte[] { 0x90, 0x90 });

            //Adding the new one
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 3E800000
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 3E800000
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX2));
            //mov ecx,0000000C
            CaveMemory.Write_StrBytes("B9 0C 00 00 00");
            //mul ecx
            CaveMemory.Write_StrBytes("F7 E1");
            //add eax, [☺SPRITE_0 ID]
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes((BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Num0)));  
            //mov edi, eax
            CaveMemory.Write_StrBytes("8B F8");
            //cmp [esp+2C], 1
            CaveMemory.Write_StrBytes("83 7C 24 2C 01");            
            //jne Draw
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX2));
            //Draw:
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 00000000
            CaveMemory.Write_StrBytes("68 00 00 00 00");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsDigit1SpriteInGame_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_ReplaceCreditsDigit1SpriteInGame() => Adding codecave at " + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsDigit1SpriteInGame_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsDigit1SpriteInGame_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);        
        }
        private void Mod_ReplaceCreditsDigit2SpriteInGame()
        {
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 3E800000
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 3E800000
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX2 + 10.0f));
            //mov eax, esi
            CaveMemory.Write_StrBytes("8B C6");
            //mov ecx,0000000C
            CaveMemory.Write_StrBytes("B9 0C 00 00 00");
            //mul ecx
            CaveMemory.Write_StrBytes("F7 E1");
            //add eax, [☺SPRITE_0 ID]
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes((BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Num0)));
            //mov edi, eax
            CaveMemory.Write_StrBytes("8B F8");
            //cmp [esp+2C], 1
            CaveMemory.Write_StrBytes("83 7C 24 2C 01");  
            //jne Draw
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX2 + 10.0f));
            //Draw:
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 00000000
            CaveMemory.Write_StrBytes("68 00 00 00 00");              
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsDigit2SpriteInGame_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_ReplaceCreditsDigit2SpriteInGame() => Adding codecave at " + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsDigit2SpriteInGame_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ReplaceCreditsDigit2SpriteInGame_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);                
        }
        
        //Sae thing as above, but we will check Credits counter to select either PRESS START or INSERT COIN(S)
        private void Mod_ReplacePushStartButtonSpriteInGame()
        {
            //Adding the new one
            Codecave CaveMemory = new Codecave(_Process, _Process.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_UpperSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X));
            //mov eax,edi
            CaveMemory.Write_StrBytes("8B C7");
            //mov ecx, 0x2C
            CaveMemory.Write_StrBytes("B9 2C 02 00 00");
            //mul ecx
            CaveMemory.Write_StrBytes("F7 E1");
            //add eax, [Player1Status]
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Player1GameStatus_Offset));
            //cmp edi,01
            CaveMemory.Write_StrBytes("83 FF 01");
            //mov edi, [PRESS START SPRITE ID]
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.PressStart));
            //jne Next
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X));
            //Next:
            //cmp byte ptr [FREEPLAY],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _FlagFreeplay_Offset));
            CaveMemory.Write_StrBytes("01");
            //je draw
            CaveMemory.Write_StrBytes("74 1E");
            //xor ecx,ecx
            CaveMemory.Write_StrBytes("31 C9");
            //mov cl,[CREDITS TO CONTINUE]
            CaveMemory.Write_StrBytes("8A 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CreditsToContinue_Offset));
            //cmp [eax], 9
            CaveMemory.Write_StrBytes("83 38 09");
            //jne Next2
            CaveMemory.Write_StrBytes("75 06");
            //mov cl, [CREDITS_TO_START]
            CaveMemory.Write_StrBytes("8A 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CreditsToStart_Offset));
            //Next2
            //cmp [CREDITS], ecx
            CaveMemory.Write_StrBytes("39 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            //jg draw
            CaveMemory.Write_StrBytes("7D 03");
            //add edi, 0x18
            CaveMemory.Write_StrBytes("83 C7 18");
            //draw:
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 00 00");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_Process_MemoryBaseAddress + _ReplacePressStartButtonSpriteInGame_Injection.Injection_ReturnOffset);

            Logger.WriteLog("Mod_ReplacePushStartButtonSpriteInGame() => Adding codecave at " + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _Process.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_Process_MemoryBaseAddress + _ReplacePressStartButtonSpriteInGame_Injection.Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_Process_MemoryBaseAddress + _ReplacePressStartButtonSpriteInGame_Injection.Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);               
        }

        //By default, if not on freeplay, the game display directly a Game Over screen if there is not enough Credits to continue the game
        //This mod will enable the display of the Continue Screen so that a player can insert more Credits to continue            
        private void Mod_ForceContinueDispay()
        {
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _EnableContinueScreen_Offset, new byte[] { 0xEB, 0x05 });
        }        

        //Changing opacity to "0" on the sprite display
        private void Mod_HideCrosshairs()
        {
            WriteByte((UInt32)_Process_MemoryBaseAddress + _HideCrosshairs_Offset, 0x00);
        }   

        //On exit (close window Cross, not ALT+F4) there's a popup that we can remove
        private void Mod_ConfirmExitGame()
        {
            SetNops((UInt32)_Process_MemoryBaseAddress, _Nop_ConfirmExitGame);
            WriteBytes((UInt32)_Process_MemoryBaseAddress + _DisableExitConfirm_Offset, new byte[] { 0xE9, 0xE5, 0x00, 0x00, 0x00 });
        }
        
        #endregion

        #region MD5 Verification

        /// <summary>
        /// Compute the MD5 hash of the target executable and compare it to the known list of MD5 Hashes
        /// This can be usefull if people are using some unknown dump with different memory, 
        /// or a wrong version of emulator
        /// This is absolutely not blocking, just for debuging with output log
        /// </summary>
        protected void CheckExeMd5()
        {
            CheckMd5(_Process.MainModule.FileName);
        }
        protected void CheckMd5(String TargetFileName)
        {
            GetMd5HashAsString(TargetFileName);
            Logger.WriteLog("CheckMd5() => MD5 hash of " + TargetFileName + " = " + _TargetProcess_Md5Hash);

            String FoundMd5 = String.Empty;
            foreach (KeyValuePair<String, String> pair in _KnownMd5Prints)
            {
                if (pair.Value == _TargetProcess_Md5Hash)
                {
                    FoundMd5 = pair.Key;
                    break;
                }
            }

            if (FoundMd5 == String.Empty)
            {
                Logger.WriteLog(@"CheckMd5() => /!\ MD5 Hash unknown, the mod may not work correctly with this target /!\");
            }
            else
            {
                Logger.WriteLog("CheckMd5() => MD5 Hash is corresponding to a known target = " + FoundMd5);
            }

        }

        /// <summary>
        /// Compute the MD5 hash from the target file.
        /// </summary>
        /// <param name="FileName">Full  filepath of the targeted executable.</param>
        private void GetMd5HashAsString(String FileName)
        {
            if (File.Exists(FileName))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(FileName))
                    {
                        var hash = md5.ComputeHash(stream);
                        _TargetProcess_Md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
        }

        #endregion

        #region MemoryData Loading

        /// <summary>
        /// Read memory values in .cfg file, whose name depends on the MD5 hash of the targeted exe.
        /// Mostly used for PC games
        /// </summary>
        /// <param name="GameData_Folder"></param>
        protected virtual void ReadGameDataFromMd5Hash()
        {
            String ConfigFile = AppDomain.CurrentDomain.BaseDirectory + MEMORY_DATA_FOLDER + @"\" + _TargetProcess_Md5Hash + ".cfg";
            if (File.Exists(ConfigFile))
            {
                Logger.WriteLog("ReadGameDataFromMd5Hash() => Reading game memory setting from " + ConfigFile);
                using (StreamReader sr = new StreamReader(ConfigFile))
                {
                    String line;
                    String FieldName = String.Empty;
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        String[] buffer = line.Split('=');
                        if (buffer.Length > 1)
                        {
                            try
                            {
                                FieldName = "_" + buffer[0].Trim();
                                if (buffer[0].Contains("Nop"))
                                {
                                    NopStruct n = new NopStruct(buffer[1].Trim());
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
                                    Logger.WriteLog(FieldName + " successfully set to following NopStruct : 0x" + n.MemoryOffset.ToString("X8") + "|" + n.Length.ToString());
                                }
                                else if (buffer[0].Contains("Injection"))
                                {
                                    InjectionStruct i = new InjectionStruct(buffer[1].Trim());
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, i);
                                    Logger.WriteLog(FieldName + " successfully set to following InjectionStruct : 0x" + i.Injection_Offset.ToString("X8") + "|" + i.Length.ToString());
                                }
                                else
                                {
                                    UInt32 v = UInt32.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, v);
                                    Logger.WriteLog(FieldName + " successfully set to following value : 0x" + v.ToString("X8"));
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog("ReadGameDataFromMd5Hash() => Error reading game data for " + FieldName + " : " + ex.Message.ToString());
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                Logger.WriteLog("ReadGameDataFromMd5Hash() => Memory File not found : " + ConfigFile);
            }
        }

        #endregion

        #region Memory Hack x86

        /// <summary>
        /// Defines how many NOP to write at a given Memory offset
        /// </summary>
        public struct NopStruct
        {
            public UInt32 MemoryOffset;
            public UInt32 Length;

            public NopStruct(UInt32 Offset, UInt32 NopLength)
            {
                MemoryOffset = Offset;
                Length = NopLength;
            }

            public NopStruct(String OffsetAndNumber)
            {
                MemoryOffset = 0;
                Length = 0;
                if (OffsetAndNumber != null)
                {
                    try
                    {
                        Length = UInt32.Parse((OffsetAndNumber.Split('|'))[1]);
                        MemoryOffset = UInt32.Parse((OffsetAndNumber.Split('|'))[0].Substring(2).Trim(), System.Globalization.NumberStyles.HexNumber);
                    }
                    catch
                    {
                        Logger.WriteLog("Impossible to load NopStruct from following String : " + OffsetAndNumber);
                    }
                }
            }
        }

        /// <summary>
        /// Defines an injection Memory zone and it's length
        /// </summary>
        public struct InjectionStruct
        {
            public UInt32 Injection_Offset;
            public UInt32 Injection_ReturnOffset;
            public UInt32 Length;

            public InjectionStruct(UInt32 Offset, UInt32 InjectionLength)
            {
                Injection_Offset = Offset;
                Length = InjectionLength;
                Injection_ReturnOffset = Offset + Length;
            }

            public InjectionStruct(String OffsetAndNumber)
            {
                Injection_Offset = 0;
                Length = 0;
                Injection_ReturnOffset = 0;
                if (OffsetAndNumber != null)
                {
                    try
                    {
                        Length = UInt32.Parse((OffsetAndNumber.Split('|'))[1]);
                        Injection_Offset = UInt32.Parse((OffsetAndNumber.Split('|'))[0].Substring(2).Trim(), System.Globalization.NumberStyles.HexNumber);
                        Injection_ReturnOffset = Injection_Offset + Length;
                    }
                    catch
                    {
                        Logger.WriteLog("Impossible to load InjectionStruct from following String : " + OffsetAndNumber);
                    }
                }
            }
        }

        protected Byte ReadByte(UInt32 Address)
        {
            byte[] Buffer = { 0 };
            UInt32 bytesRead = 0;
            if (!Win32API.ReadProcessMemory(_ProcessHandle, Address, Buffer, 1, ref bytesRead))
            {
                Logger.WriteLog("Cannot read memory at address 0x" + Address.ToString("X8"));
            }
            return Buffer[0];
        }

        protected Byte[] ReadBytes(UInt32 Address, UInt32 BytesCount)
        {
            byte[] Buffer = new byte[BytesCount];
            UInt32 bytesRead = 0;
            if (!Win32API.ReadProcessMemory(_ProcessHandle, Address, Buffer, (UInt32)Buffer.Length, ref bytesRead))
            {
                Logger.WriteLog("Cannot read memory at address 0x" + Address.ToString("X8"));
            }
            return Buffer;
        }

        protected UInt32 ReadPtr(UInt32 PtrAddress)
        {
            byte[] Buffer = ReadBytes(PtrAddress, 4);
            return BitConverter.ToUInt32(Buffer, 0);
        }

        protected UInt32 ReadPtrChain(UInt32 BaseAddress, UInt32[] Offsets)
        {
            byte[] Buffer = ReadBytes(BaseAddress, 4);
            UInt32 Ptr = BitConverter.ToUInt32(Buffer, 0);

            if (Ptr == 0)
            {
                return 0;
            }
            else
            {
                for (int i = 0; i < Offsets.Length; i++)
                {
                    Buffer = ReadBytes(Ptr + Offsets[i], 8);
                    Ptr = BitConverter.ToUInt32(Buffer, 0);

                    if (Ptr == 0)
                        return 0;
                }
            }

            return Ptr;
        }

        protected bool WriteByte(UInt32 Address, byte Value)
        {
            UInt32 bytesWritten = 0;
            Byte[] Buffer = { Value };
            if (Win32API.WriteProcessMemory(_ProcessHandle, Address, Buffer, 1, ref bytesWritten))
            {
                if (bytesWritten == 1)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        protected bool WriteBytes(UInt32 Address, byte[] Buffer)
        {
            UInt32 bytesWritten = 0;
            if (Win32API.WriteProcessMemory(_ProcessHandle, Address, Buffer, (UInt32)Buffer.Length, ref bytesWritten))
            {
                if (bytesWritten == Buffer.Length)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        protected void SetNops(UInt32 BaseAddress, NopStruct Nop)
        {
            for (UInt32 i = 0; i < Nop.Length; i++)
            {
                UInt32 Address = (UInt32)BaseAddress + Nop.MemoryOffset + i;
                if (!WriteByte(Address, 0x90))
                {
                    Logger.WriteLog("Impossible to NOP address 0x" + Address.ToString("X8"));
                    break;
                }
            }
        }

        #endregion                

    }
}

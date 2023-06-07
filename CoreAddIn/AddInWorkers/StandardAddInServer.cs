﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Inventor;

namespace CoreAddIn
{
    [ComVisible(true)]
    [Guid(Globals.g_simpleAddInClientID)]
    [ProgId("CoreAddIn.StandardAddInServer")]
    public class StandardAddInServer : ApplicationAddInServer
    {
        // *********************************************************************************
        // * The two declarations below are related to adding buttons to Inventor's UI.
        // * They can be deleted if this add-in doesn't have a UI and only runs in the 
        // * background handling events.
        // *********************************************************************************

        // Declaration of the object for the UserInterfaceEvents to be able to handle
        // if the user resets the ribbon so the button can be added back in.

        private UserInterfaceEvents? _uiEvents;

        public UserInterfaceEvents? UiEvents
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _uiEvents;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_uiEvents != null)
                {
                    _uiEvents.OnResetRibbonInterface -= UiEvents_OnResetRibbonInterface;
                }

                _uiEvents = value;
                if (_uiEvents != null)
                {
                    _uiEvents.OnResetRibbonInterface += UiEvents_OnResetRibbonInterface;
                }
            }
        }

        // Declaration of the button definition with events to handle the click event.
        // For additional commands this declaration along with other sections of code
        // that apply to the button can be duplicated from this example.
        public class UI_Button
        {
            private ButtonDefinition? _btnDefinition;

            public ButtonDefinition? BtnDefinition
            {
                [MethodImpl(MethodImplOptions.Synchronized)]
                get
                {
                    return _btnDefinition;
                }

                [MethodImpl(MethodImplOptions.Synchronized)]
                set
                {
                    if (_btnDefinition != null)
                    {
                        _btnDefinition.OnExecute -= BtnDefinition_OnExecute;
                    }

                    _btnDefinition = value;
                    if (_btnDefinition != null)
                    {
                        _btnDefinition.OnExecute += BtnDefinition_OnExecute;
                    }
                }
            }

            private void BtnDefinition_OnExecute(NameValueMap Context)
            {
                // Link button clicks to their respective commands.
                switch (BtnDefinition?.InternalName)
                {
                    case "dw_NewWithPathFromPart":

                        if (Globals.InvApp != null)
                            System.Windows.MessageBox.Show($"{ Globals.InvApp.ActiveDocument.DisplayName } is the part name (test:09)");
                        else
                            throw new NullReferenceException($"{ nameof(Globals.InvApp) } was null. We somehow do not have a valid Inventor Application reference");
                                               
                        return;

                    default:
                        return;
                }
            }
        }

        public delegate ButtonDefinition CreateButton(string display_text, string internal_name, string icon_path);
        public ButtonDefinition ButtonTemplate(string display_text, string internal_name, string icon_path)
        {
            var myButton = new UI_Button
            {
                BtnDefinition = AddinUtilities.CreateButtonDefinition(display_text, internal_name, "", icon_path)
            };

            return myButton.BtnDefinition ?? throw new NullReferenceException($"In { nameof(ButtonTemplate) }, { nameof(myButton.BtnDefinition) }  was null and should not be");
        }

        // Declare all buttons here
        ButtonDefinition? ButtonShowPartName;

        // This method is called by Inventor when it loads the AddIn. The AddInSiteObject provides access  
        // to the Inventor Application object. The FirstTime flag indicates if the AddIn is loaded for
        // the first time. However, with the introduction of the ribbon this argument is always true.
        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            try
            {
                // Initialize AddIn members.
                Globals.InvApp = addInSiteObject.Application;

                // Connect to the user-interface events to handle a ribbon reset.
                UiEvents = Globals.InvApp.UserInterfaceManager.UserInterfaceEvents;

                // *********************************************************************************
                // * The remaining code in this Sub is all for adding the add-in into Inventor's UI.
                // * It can be deleted if this add-in doesn't have a UI and only runs in the 
                // * background handling events.
                // *********************************************************************************

                // ButtonName = create_button(display_text, internal_name, icon_path)
                var createButton = new CreateButton(ButtonTemplate);

                ButtonShowPartName = createButton("Core Addin Show PartName", "dw_NewWithPathFromPart", @"Resources\Buttons\ManualInputIcon");

                // Add to the user interface, if it's the first time.
                // If this add-in doesn't have a UI but runs in the background listening
                // to events, you can delete this
                if (firstTime)
                {
                    AddToUserInterface();
                }


            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Unexpected failure in the activation of the add-in \"INVENTOR_DrawingFiller\"" + System.Environment.NewLine + System.Environment.NewLine + ex.Message);
            }
        }

        // This method is called by Inventor when the AddIn is unloaded. The AddIn will be
        // unloaded either manually by the user or when the Inventor session is terminated.
        public void Deactivate()
        {
            // Release objects.
            ButtonShowPartName = null;

            UiEvents = null;
            Globals.InvApp = null;

            //MessageBox.Show("Addin unloaded");

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // This property is provided to allow the AddIn to expose an API of its own to other 
        // programs. Typically, this  would be done by implementing the AddIn's API
        // interface in a class and returning that class object through this property.
        // Typically it's not used, like in this case, and returns Nothing.
        public object? Automation
        {
            get
            {
                return null;
            }
        }

        // Note:this method is now obsolete, you should use the 
        // ControlDefinition functionality for implementing commands.
        public void ExecuteCommand(int commandID)
        {
        }


        // Adds whatever is needed by this add-in to the user-interface.  This is 
        // called when the add-in loaded and also if the user interface is reset.
        private void AddToUserInterface()
        {
            #region Ribbon, Tabs, Panel setup
            // Get the ribbon. (more buttons can be added to various ribbons within this single addin)
            // Ribbons:
            // ZeroDoc
            // Part
            // Assembly
            // Drawing
            // Presentation
            // iFeatures
            // UnknownDocument
            //Ribbon asmRibbon = Globals.invApp.UserInterfaceManager.Ribbons["Assembly"];
            //Ribbon prtRibbon = Globals.invApp.UserInterfaceManager.Ribbons["Part"];

            Ribbon partRibbon;

            if (Globals.InvApp != null)
                partRibbon = Globals.InvApp.UserInterfaceManager.Ribbons["Part"];
            else
                throw new NullReferenceException($"{ nameof(Globals.InvApp) } was null. We somehow do not have a valid Inventor Application reference");


            // Set up Tabs.
            // tab = setup_panel(display_name, internal_name, inv_ribbon)
            RibbonTab MyTab_part;
            MyTab_part = SetupTab("Tools", "id_TabTools", partRibbon);

            // Set up Panels.
            // panel = setup_panel(display_name, internal_name, ribbon_tab)

            RibbonPanel MyPanel_part;
            MyPanel_part = SetupPanel("Core AddIn Test", "id_TabTools", MyTab_part);

            #endregion

            // Part panel buttons
            if (!(ButtonShowPartName == null))
            {
                MyPanel_part.CommandControls.AddButton(ButtonShowPartName, false);
            }

        }


        private static RibbonTab SetupTab(string display_name, string internal_name, Ribbon inv_ribbon)
        {
            RibbonTab? setup_tabRet;
            RibbonTab? ribbon_tab = null;

            try
            {
                ribbon_tab = inv_ribbon.RibbonTabs[internal_name];
            }
            catch (Exception)
            {
                //MessageBox.Show("Couldn't set up tab: " + ex.Message);
            }

            if (ribbon_tab == null)
            {
                ribbon_tab = inv_ribbon.RibbonTabs.Add(display_name, internal_name, Globals.g_addInClientID);
            }

            setup_tabRet = ribbon_tab;

            return setup_tabRet;
        }


        private static RibbonPanel SetupPanel(string display_name, string internal_name, RibbonTab ribbon_tab)
        {
            RibbonPanel? setup_panelRet;
            RibbonPanel? ribbon_panel = null;

            try
            {
                ribbon_panel = ribbon_tab.RibbonPanels[internal_name];
            }
            catch (Exception)
            {
                //MessageBox.Show("Couldn't set up panel: " + ex.Message);
            }

            if (ribbon_panel == null)
            {
                ribbon_panel = ribbon_tab.RibbonPanels.Add(display_name, internal_name, Globals.g_addInClientID);
            }

            setup_panelRet = ribbon_panel;

            return setup_panelRet;
        }


        private void UiEvents_OnResetRibbonInterface(NameValueMap Context)
        {
            // The ribbon was reset, so add back the add-ins user-interface.
            AddToUserInterface();
        }
    }

    public static class Globals
    {
        // Inventor application object.
        public static Inventor.Application? InvApp { get; internal set; }

        // The unique ID for this add-in.  If this add-in is copied to create a new add-in
        // you need to update this ID along with the ID in the .manifest file, the .addin file
        // and create a new ID for the typelib GUID in AssemblyInfo.vb
        public const string g_simpleAddInClientID = "5d437d7f-a9a9-4e01-a509-bcd8cced82e1";
        public const string g_addInClientID = "{" + g_simpleAddInClientID + "}";
    }
}

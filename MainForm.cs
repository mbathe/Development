//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
//
// Version:         $Revision: 10801 $,
//                  $Date: 2019-06-13 15:20:59 +0300 (to, 13 kesä 2019) $
//                  $Author: gurux01 $
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// More information of Gurux DLMS/COSEM Director: https://www.gurux.org/GXDLMSDirector
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using GXDLMS.ManufacturerSettings;
using System.Diagnostics;
using System.Threading;
using GXDLMS.Common;
using Gurux.Common;
using MRUSample;
using Gurux.DLMS.ManufacturerSettings;
using Gurux.DLMS.Objects;
using Gurux.DLMS;
using System.Linq;
using Gurux.DLMS.Enums;
using Gurux.DLMS.UI;
using Gurux.Net;
using System.Text;
using static System.Windows.Forms.ListView;
using System.Reflection;
using System.Deployment.Application;
using System.Resources;
using Microsoft.Win32;

namespace GXDLMSDirector
{
    public partial class MainForm : Form
    {
        GXNet events;

        /// <summary>
        /// Active DC.
        /// </summary>
        IGXDataConcentrator activeDC;

        /// <summary>
        /// Events translator.
        /// </summary>
        readonly GXDLMSTranslator eventsTranslator = new GXDLMSTranslator(TranslatorOutputType.SimpleXml);
        GXByteBuffer eventsData = new GXByteBuffer();
        delegate void ShowLastErrorsEventHandler(GXDLMSObject target);

        /// <summary>
        /// Log translator.
        /// </summary>
        GXDLMSTranslator traceTranslator;
        Form[] addInForms = null;

        delegate void CheckUpdatesEventHandler(MainForm form);
        GXAsyncWork TransactionWork;
        Dictionary<Type, List<IGXDLMSView>> Views;
        String path;
        GXManufacturerCollection Manufacturers;
        System.Collections.Hashtable ObjectTreeItems = new System.Collections.Hashtable();
        SortedList<int, GXDLMSObject> SelectedListItems = new SortedList<int, GXDLMSObject>();
        System.Collections.Hashtable ObjectListItems = new System.Collections.Hashtable();
        System.Collections.Hashtable ObjectValueItems = new System.Collections.Hashtable();
        System.Collections.Hashtable DeviceListViewItems = new System.Collections.Hashtable();
        GXDLMSMeterCollection Devices;
        IGXDLMSView SelectedView = null;
        bool Dirty = false;
        /*------------------------------CREATION DE LA LISTE QUI CONTIENDRA LES OBJET EN ROUTINE---------------*/
        public static List<String> routineList = new List<string>();

        public static Dictionary<string, List<String>> routine = new Dictionary<string, List<String>>();

        public static Dictionary<string, Dictionary<String,int>> routine2 = new Dictionary<string, Dictionary<String, int>>();

        static bool actifChronometre = false;
        static bool actifBoucle = false;
        static DateTime timeDebut;
        int a1 = 1; int b1 = 0; int c1 = 0; int d1 = 0; int f1 = 0;
        int second = 0;// permet de definir le nombre de seconde du timer2 qui servira a savoir s'il est minuit
        public static bool activationMinui = false;
        public static bool actifThread = false;
        public static String nomDos = "nouveauFichier";
        public static DateTime dateLecture;
     
        int i = 0;
        public static Thread myThread;
        public static Thread treadMinui;
        public static Thread tread10Minutes;
        public static Thread treadHeure;
        public static int nombreDeSeconde = 0;
        public static int indicateurBoucle = 0; //permet de savoir le numero du blouclage
        public static bool activationLectureBoucle = false;
        public static bool entreMinui = false;
        public static bool entreHeur = false;
        public static bool entre10Minutes = false;
        public static int tempsDeBoucle = 0;
        public static bool premierFois = true;
        public static bool actifSauvegarde = false;
        int mili = 0; int sec = 0;
        SettingSerial settingSerial = new SettingSerial();
        public static  System.Threading.Timer timer;
        public static System.Threading.Timer timerThread;
        System.Windows.Forms.Button button = new System.Windows.Forms.Button();//bouton a ajouter pour exporter les courbe de charge
        public static bool pasConnexion=false;
        public static bool variationModeEtat = false;//PERME DE SAVOIR SI LE MODE D'ETAT A VARIER POUR CHARGE LE DIAGRAMME DE DIAGNOSTIQUE
        public static String visuel = "";
        public static List<String> diagnosticList = new List<String>() { "1.1.81.7.40.255", "1.1.81.7.51.255", "1.1.81.7.62.255", "1.1.31.7.0.255", "1.1.71.7.0.255", "1.1.32.7.0.255", "1.1.52.7.0.255", "1.1.72.7.0.255", "1.1.81.7.10.255", "1.1.81.7.2.255", "1.1.81.7.21.255" };
        public static String valeurModeEtatCourant = "0000000000000000000000000000000000000000";
        public static bool messeageErreur = true; //permet de ne pas afficher le message d'erreur lorque l'on n'est dans la boucle
        /*------------------------------CREATION DE LA LISTE QUI CONTIENDRA LES OBJET EN ROUTINE---------------*/

        delegate void DirtyEventHandler(bool dirty);
        private MRUManager mruManager = null;


        /// <summary>
        /// Used trace level.
        /// </summary>
        private byte traceLevel = 0;
        /// <summary>
        /// Used notification level.
        /// </summary>
        private byte notificationLevel = 0;

        /// <summary>
        /// Received trace data.
        /// </summary>
        GXByteBuffer receivedTraceData = new GXByteBuffer();


        private static GXDLMSDevice GetDevice(GXDLMSObject item)
        {
            return item.Parent.Tag as GXDLMSDevice;
        }

        void OnDirty(bool dirty)
        {
            //Dirty is not used with DC.
            Dirty = dirty && activeDC == null;
            this.Text = Properties.Resources.GXDLMSDirectorTxt + " " + path;
            if (Dirty)
            {
                this.Text += " *";
            }
        }

        internal void SetDirty(bool dirty)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new DirtyEventHandler(OnDirty), dirty);
            }
            else
            {
                OnDirty(dirty);
            }
        }

        public static void InitMain()
        {
            //Debug traces are written only log file.
            if (!Debugger.IsAttached)
            {
                Trace.Listeners.Clear();
            }
            DirectoryInfo di = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName((GXLogWriter.LogPath)));
            if (!di.Exists)
            {
                di.Create();
            }
            GXLogWriter.ClearLog();
            Trace.Listeners.Add(new TextWriterTraceListener(GXLogWriter.LogPath));
            Trace.AutoFlush = true;
            Trace.IndentSize = 4;
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            MainForm form = new MainForm();
          
            Application.Run(form);
          
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Do not catch errors here.
        /// If error is occurred exception is shown and application is closed.
        /// </remarks>
        public MainForm()
        {
            InitializeComponent();
            //initialisationVariable();
            // timer2.Start();
           // button1.Text = "Start";
           entreMinui = false;
            entreHeur = false;
            entre10Minutes = false;
            CancelBtn.Enabled = false;
            traceTranslator = new GXDLMSTranslator(TranslatorOutputType.SimpleXml);

            Devices = new GXDLMSMeterCollection();
            ObjectValueView.Visible = DeviceInfoView.Visible = DeviceList.Visible = false;
            ObjectValueView.Dock = tabControl2.Dock = DeviceList.Dock = DeviceInfoView.Dock = DockStyle.Fill;
            ProgressBar.Visible = false;
            UpdateDeviceUI(null, DeviceState.None);
            NewMnu_Click(null, null);
            mruManager = new MRUManager(RecentFilesMnu);
            mruManager.OnOpenMRUFile += new OpenMRUFileEventHandler(this.OnOpenMRUFile);
            //Add access right view.
            Accessrights.AutoGenerateColumns = false;
            Accessrights.AutoSize = true;
            Accessrights.Columns.Clear();
            DataGridViewColumn column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "Index";
            column.Name = "Attribute Index";
            column.ReadOnly = true;
            Accessrights.Columns.Add(column);
            column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "Name";
            column.Name = "Description";
            column.ReadOnly = true;
            Accessrights.Columns.Add(column);
            Accessrights.Columns.Add(CreateAccessRightColumns());
            Accessrights.Columns.Add(CreateStaticColumns());
            Accessrights.Columns.Add(CreateTypeColumns("Type"));
            Accessrights.Columns.Add(CreateTypeColumns("UIType"));

            Accessrights.CellValueChanged += Accessrights_CellValueChanged;
            Accessrights.AllowUserToAddRows = false;
            Accessrights.AllowUserToDeleteRows = false;
            Accessrights.AutoSize = true;

            //Add property error view.
            PropertyErrorView.AutoGenerateColumns = false;
            PropertyErrorView.AutoSize = true;
            PropertyErrorView.Columns.Clear();
            column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "Index";
            column.Name = "Attribute Index";
            column.ReadOnly = true;
            PropertyErrorView.Columns.Add(column);
            column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "Name";
            column.Name = "Description";
            column.ReadOnly = true;
            PropertyErrorView.Columns.Add(column);
            column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "Error";
            column.Name = "Last error";
            column.ReadOnly = true;
            PropertyErrorView.Columns.Add(column);
            PropertyErrorView.AllowUserToAddRows = false;
            PropertyErrorView.AllowUserToDeleteRows = false;
            PropertyErrorView.AutoSize = true;
            this.Shown += new System.EventHandler(ChargerFichier);// permet de charge le fichier de configuration 
             

        }

        public void OnProgress(object sender, string description, int current, int maximium)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ProgressEventHandler(OnProgress), sender, description, current, maximium);
            }
            else
            {
                if (this.Created)
                {
                    if (current > maximium)
                    {
                        maximium = current;
                    }
                    StatusLbl.Text = description;
                    ProgressBar.Maximum = maximium;
                    ProgressBar.Value = current;
                }
            }
        }

        public void StatusChanged(object sender, DeviceState state)
        {
            UpdateDeviceUI((GXDLMSDevice)sender, state);
        }

        public void OnStatusChanged(object sender, DeviceState state)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new StatusEventHandler(StatusChanged), sender, state);
            }
            else
            {
                StatusChanged(sender, state);
            }
        }

        /// <summary>
        /// Is write enabled.
        /// </summary>
        /// <remarks>
        /// Write is enabled for only some interfaces.
        /// </remarks>
        /// <returns></returns>
        void UpdateWriteEnabled()
        {
            if (SelectedView != null && SelectedView.Target.GetDirtyAttributeIndexes().Length != 0 && ReadCMnu.Enabled)
            {
                WriteBtn.Enabled = WriteMnu.Enabled = true;
            }
            else
            {
                WriteBtn.Enabled = WriteMnu.Enabled = false;
            }
        }

        GXDLMSDevice selectedDevice;

        void UpdateDeviceUI(GXDLMSDevice device, DeviceState state)
        {
            if (activeDC != null)
            {
                ImportMenu.Enabled = ExportMenu.Enabled = ConnectCMnu.Enabled = ConnectBtn.Enabled = ConnectMnu.Enabled = false;
                ReadCMnu.Enabled = ReadBtn.Enabled = RefreshMnu.Enabled = ReadMnu.Enabled = true;
                DisconnectCMnu.Enabled = DisconnectMnu.Enabled = ConnectBtn.Checked = false;
                DeleteCMnu.Enabled = DeleteMnu.Enabled = DeleteBtn.Enabled = OptionsBtn.Enabled = true;
                ReadObjectMnu.Enabled = false;
                addRoutine.Enabled = true;
            }
            else
            {
                if (selectedDevice != null && selectedDevice != device)
                {
                    receivedTraceData.Clear();
                    TraceView.ResetText();
                    selectedDevice.OnTrace = null;
                    selectedDevice.OnEvent = null;
                    selectedDevice = null;
                }
                if (device != null)
                {
                    selectedDevice = device;
                    device.OnTrace = OnTrace;
                    device.OnEvent = Events_OnReceived;
                }
                ImportMenu.Enabled = ExportMenu.Enabled = device != null;
                ConnectCMnu.Enabled = ConnectBtn.Enabled = ConnectMnu.Enabled = (state & DeviceState.Initialized) != 0;
                ReadCMnu.Enabled = ReadBtn.Enabled = RefreshMnu.Enabled = ReadMnu.Enabled = (state & DeviceState.Connected) == DeviceState.Connected;
                DisconnectCMnu.Enabled = DisconnectMnu.Enabled = ConnectBtn.Checked = (state & DeviceState.Connected) == DeviceState.Connected;
                DeleteCMnu.Enabled = DeleteMnu.Enabled = DeleteBtn.Enabled = OptionsBtn.Enabled = state == DeviceState.Initialized;
                ReadObjectMnu.Enabled = ReadMnu.Enabled && (device.Comm.client.NegotiatedConformance & Conformance.MultipleReferences) != 0;
                UpdateWriteEnabled();
            }
        }


        private void SelectItem(object obj)
        {
            //  textBox1.Text += " type de objet : -->  "+obj.GetType().ToString()+" -->fin du type";
            try
            {
                if (activeDC != null && !(obj is GXDLMSMeterCollection))
                {
                    DeviceInfoView.TabPages.Clear();
                    if (addInForms != null)
                    {
                        foreach (Form it in addInForms)
                        {
                            it.Close();
                            it.Dispose();
                        }
                    }
                    addInForms = activeDC.CustomPages(obj, activeDC);
                    if (addInForms != null)
                    {
                        DeviceInfoView.Visible = true;
                        DeviceInfoView.TabPages.Clear();
                        DeviceList.Visible = tabControl2.Visible = ObjectValueView.Visible = false;
                        foreach (Form f in addInForms)
                        {
                            TabPage tp = new TabPage(f.Text);
                            DeviceInfoView.TabPages.Add(tp);
                            foreach (Control it in f.Controls)
                            {
                                tp.Controls.Add(it);
                            }
                        }
                    }
                }
                if (DeviceInfoView.Controls.Count == 0)
                {
                    DeviceInfoView.Controls.Add(this.tabPage4);
                    DeviceInfoView.Controls.Add(this.tabPage5);
                }
                if (SelectedView != null && SelectedView.Target != null)
                {
                    SelectedView.Target.OnChange -= new ObjectChangeEventHandler(DLMSItemOnChange);
                }
                //Do not shown list if there is only one item in the list.
                if (obj is GXDLMSObjectCollection)
                {
                    GXDLMSObjectCollection items = obj as GXDLMSObjectCollection;
                    if (items.Count == 1)
                    {
                        obj = items[0];
                    }
                }

                DeviceList.Visible = obj is GXDLMSMeterCollection;
                //If device is selected.
                DeviceInfoView.Visible = obj is GXDLMSMeter;
                tabControl2.Visible = obj is GXDLMSObject;
                ObjectValueView.Visible = obj is GXDLMSObjectCollection;
                if (DeviceList.Visible)
                {
                    SaveAsTemplateBtn.Enabled = false;
                    DeviceState state = Devices.Count == 0 ? DeviceState.None : DeviceState.Initialized;
                    UpdateDeviceUI(null, state);
                    DeleteMnu.Enabled = DeleteBtn.Enabled = OptionsBtn.Enabled = false;
                    DeviceList.BringToFront();
                }
                else if (DeviceInfoView.Visible)
                {
                    SaveAsTemplateBtn.Enabled = true;
                    GXDLMSMeter dev = (GXDLMSMeter)obj;
                    GXDLMSDevice d = obj as GXDLMSDevice;
                    DeviceGb.Text = dev.Name;
                    ClientAddressValueLbl.Text = dev.ClientAddress.ToString();
                    LogicalAddressValueLbl.Text = dev.LogicalAddress.ToString();
                    PhysicalAddressValueLbl.Text = dev.PhysicalAddress.ToString();
                    if (d != null)
                    {
                        ManufacturerValueLbl.Text = d.Manufacturers.FindByIdentification(dev.Manufacturer).Name;
                        ProposedConformanceTB.Text = d.Comm.client.ProposedConformance.ToString();
                        NegotiatedConformanceTB.Text = d.Comm.client.NegotiatedConformance.ToString();
                        UpdateDeviceUI(d, d.Status);
                    }
                    else
                    {
                        ManufacturerValueLbl.Text = dev.Manufacturer;
                        ProposedConformanceTB.Text = "";
                        NegotiatedConformanceTB.Text = "";
                    }
                    DeviceInfoView.BringToFront();
                    ErrorsView.Items.Clear();
                    foreach (GXDLMSObject it in dev.Objects)
                    {
                        SortedDictionary<int, Exception> errors = it.GetLastErrors();
                        if (errors.Count != 0)
                        {
                            int count = (it as IGXDLMSBase).GetAttributeCount() + 1;
                            int add = ErrorsView.Columns.Count;
                            count -= add;
                            for (int pos = 0; pos < count; ++pos)
                            {
                                ErrorsView.Columns.Add("Attribute " + (add + pos).ToString());
                            }
                            count = (it as IGXDLMSBase).GetAttributeCount();
                            ListViewItem lv = new ListViewItem(it.LogicalName + " " + it.Description);
                            lv.Tag = it;
                            for (int pos = 0; pos < count; ++pos)
                            {
                                lv.SubItems.Add("");
                            }
                            foreach (var e in errors)
                            {
                                lv.SubItems[e.Key].Text = e.Value.Message;
                            }
                            ErrorsView.Items.Add(lv);
                        }
                    }
                }
                else if (ObjectValueView.Visible)
                {
                    ObjectValueView.BringToFront();
                    SaveAsTemplateBtn.Enabled = true;
                    try
                    {
                        ObjectValueView.BeginUpdate();
                        ObjectValueItems.Clear();
                        ObjectValueView.Items.Clear();
                        int maxAttributeCount = ObjectValueView.Columns.Count - 2;
                        int originalCount = maxAttributeCount;
                        int maxColCount = 0;
                        int len = 0;
                        GXDLMSObjectCollection items = obj as GXDLMSObjectCollection;
                        List<ListViewItem> rows = new List<ListViewItem>(items.Count);
                        foreach (GXDLMSObject it in items)
                        {
                            object[] values = it.GetValues();
                            len = values.Length - 1;
                            if (len > maxColCount)
                            {
                                maxColCount = len;
                            }
                            if (len > maxAttributeCount)
                            {
                                for (int pos = maxAttributeCount; pos != len; ++pos)
                                {
                                    ObjectValueView.Columns.Add("Attribute " + (pos + 2).ToString());
                                    ++maxAttributeCount;
                                }
                            }
                            ListViewItem lv = new ListViewItem(it.LogicalName + " " + it.Description);
                            lv.SubItems.Add(it.ObjectType.ToString());
                            string[] texts = new string[len];
                            for (int pos = 0; pos != len; ++pos)
                            {
                                texts[pos] = "";
                            }
                            lv.SubItems.AddRange(texts);
                            ObjectValueItems[it] = lv;
                            lv.Tag = it;
                            for (int pos = 0; pos != len; ++pos)
                            {
                                UpdateValue(it, pos + 2, lv);
                            }
                            rows.Add(lv);
                        }
                        ObjectValueView.Items.AddRange(rows.ToArray());
                        int cnt = ObjectValueView.Columns.Count - maxColCount - 2;
                        for (int pos = 0; pos < cnt; ++pos)
                        {
                            ObjectValueView.Columns.RemoveAt(ObjectValueView.Columns.Count - 1);
                        }
                        for (int pos = originalCount + 1; pos < ObjectValueView.Columns.Count; ++pos)
                        {
                            ObjectValueView.Columns[pos].AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                        }
                        if (items.Count != 0)
                        {
                            GXDLMSObject obj2 = items[0] as GXDLMSObject;
                            if (obj2.Parent != null)
                            {
                                GXDLMSDevice dev = obj2.Parent.Tag as GXDLMSDevice;
                                UpdateDeviceUI(dev, dev.Status);
                            }
                        }
                    }
                    finally
                    {
                        ObjectValueView.EndUpdate();
                    }
                }
                else
                {
                    tabControl2.BringToFront();
                    SaveAsTemplateBtn.Enabled = true;
                    GXDLMSDevice d = GetDevice((GXDLMSObject)obj);
                    SelectedView = GXDlmsUi.GetView(Views, (GXDLMSObject)obj, d.Standard);
                    foreach (Control it in ObjectPanelFrame.Controls)
                    {
                        it.Hide();
                    }
                    ObjectPanelFrame.Controls.Clear();
                    SelectedView.Target = (GXDLMSObject)obj;
                    SelectedView.Target.OnChange += new ObjectChangeEventHandler(DLMSItemOnChange);
                    DeviceState s = DeviceState.Connected;
                    if (d != null)
                    {
                        s = d.Status;
                    }
                    GXDlmsUi.ObjectChanged(SelectedView, obj as GXDLMSObject, (s & DeviceState.Connected) != 0);
                    ObjectPanelFrame.Controls.Add((Form)SelectedView);
                    ((Form)SelectedView).Show();
                    ajouterBotton();
                    UpdateDeviceUI(GetDevice(SelectedView.Target), s);
                    try
                    {
                        //Show access levels of COSEM object.
                        bindingSource1.Clear();
                        //All meters don't implement association view.
                        SortedList<int, GXDLMSAttributeSettings> list = new SortedList<int, GXDLMSAttributeSettings>();
                        foreach (GXDLMSAttributeSettings it in (obj as GXDLMSObject).Attributes)
                        {
                            try
                            {
                                list.Add(it.Index, it);
                            }
                            catch
                            {
                                //Skip all errors.
                            }
                        }
                        //Add all attributes.
                        string[] names = (obj as IGXDLMSBase).GetNames();
                        for (int pos = 0; pos != (obj as IGXDLMSBase).GetAttributeCount(); ++pos)
                        {
                            if (!list.ContainsKey(pos + 1))
                            {
                                list.Add(pos + 1, new GXDLMSAttributeSettings() { Index = pos + 1, Name = names[pos] });
                            }
                            else
                            {
                                GXDLMSAttributeSettings a = list[pos + 1];
                                if (string.IsNullOrEmpty(a.Name))
                                {
                                    a.Name = names[pos];
                                }
                            }
                        }
                        foreach (var it in list)
                        {
                            bindingSource1.Add(it.Value);
                        }
                        Accessrights.DataSource = bindingSource1;
                        ShowLastErrors(obj as GXDLMSObject);
                    }
                    catch (Exception)
                    {
                        //Skip error if this fails.
                        Accessrights.DataSource = null;
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Show property errors.
        /// </summary>
        /// <param name="target">Target</param>
        private void ShowLastErrors(GXDLMSObject target)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ShowLastErrorsEventHandler(ShowLastErrors), target);
            }
            else if (SelectedView.Target == target)
            {
                bindingSource2.Clear();
                string[] names = (target as IGXDLMSBase).GetNames();
                foreach (var it in (target as GXDLMSObject).GetLastErrors())
                {
                    bindingSource2.Add(new GXLastError() { Index = it.Key, Name = names[it.Key - 1], Error = it.Value.Message });
                }
                if (bindingSource2.Count == 0)
                {
                    PropertyErrorView.DataSource = null;
                }
                else
                {
                    PropertyErrorView.DataSource = bindingSource2;
                }
            }
        }

        class GXLastError
        {
            /// <summary>
            /// Attribute index.
            /// </summary>
            public int Index
            {
                get;
                set;
            }
            /// <summary>
            /// Attribute name.
            /// </summary>
            public string Name
            {
                get;
                set;
            }
            /// <summary>
            /// Attribute exception.
            /// </summary>
            public string Error
            {
                get;
                set;
            }
        }

        private void Accessrights_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                GXDLMSAttributeSettings a = (GXDLMSAttributeSettings)bindingSource1[e.RowIndex];
                if (SelectedView.Target.Attributes.Find(a.Index) == null)
                {
                    SelectedView.Target.Attributes.Add(a);
                }
            }
            catch (Exception)
            {
                //Skip error if this fails.
            }
            SetDirty(true);
        }

        BindingSource bindingSource1 = new BindingSource();
        BindingSource bindingSource2 = new BindingSource();

        DataGridViewComboBoxColumn CreateAccessRightColumns()
        {
            DataGridViewComboBoxColumn combo = new DataGridViewComboBoxColumn();
            combo.DataSource = Enum.GetValues(typeof(AccessMode));
            combo.DataPropertyName = "Access";
            combo.Name = "Access";
            return combo;
        }

        DataGridViewColumn CreateTypeColumns(string name)
        {
            if (name == "Type")
            {
                DataGridViewColumn column = new DataGridViewTextBoxColumn();
                column.DataPropertyName = name;
                column.Name = name;
                column.ReadOnly = true;
                return column;
            }
            else
            {
                DataGridViewComboBoxColumn combo = new DataGridViewComboBoxColumn();
                combo.DataSource = Enum.GetValues(typeof(DataType));
                combo.DataPropertyName = name;
                combo.Name = name;
                return combo;
            }
        }

        DataGridViewCheckBoxColumn CreateStaticColumns()
        {
            DataGridViewCheckBoxColumn combo = new DataGridViewCheckBoxColumn();
            combo.DataPropertyName = "Static";
            combo.Name = "Static";
            return combo;
        }

        private void ObjectTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //textBox1.Text = " after selected  " + e.GetType().ToString();
            //textBox1.Text = e.Node.Name + "   ";
            //  textBox1.Text = "---->on entre dans le after selected  ici "+ e.Node.Tag.ToString();
            while (SelectedListItems.Count != 0)
            {

                GXDLMSObject it = SelectedListItems.Values[0];
                ListViewItem item = ObjectListItems[it] as ListViewItem;
                item.Selected = false;

            }
            SelectedListItems.Clear();
            SelectItem(e.Node.Tag);
            ObjectTree.Focus();
            try
            {
                this.ObjectList.ItemSelectionChanged -= new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.ObjectList_ItemSelectionChanged);
                GXDLMSObject obj = e.Node.Tag as GXDLMSObject;
                if (obj != null)
                {
                    ListViewItem item = ObjectListItems[e.Node.Tag] as ListViewItem;
                    if (item != null)
                    {
                        item.Selected = true;
                    }
                }
            }
            finally
            {
                this.ObjectList.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.ObjectList_ItemSelectionChanged);
            }
        }

        private static bool UpdateDirty(IGXDLMSView view, System.Windows.Forms.Control.ControlCollection controls, GXDLMSObject target, int index, bool dirty)
        {
            bool found = false;
            foreach (Control it in controls)
            {
                if (it is GXValueField)
                {
                    GXValueField obj = it as GXValueField;
                    if (obj.Index == index)
                    {
                        if (view.ErrorProvider != null)
                        {
                            if (dirty && index != 0)
                            {
                                view.ErrorProvider.SetError(it, GXDLMSDirector.Properties.Resources.ValueChangedTxt);
                            }
                            else
                            {
                                view.ErrorProvider.Clear();
                            }
                        }
                        found = true;
                    }
                }
                else if (it.Controls.Count != 0)
                {
                    found = UpdateDirty(view, it.Controls, target, index, dirty);
                }
                if (found)
                {
                    break;
                }
            }
            return found;
        }


        void DLMSItemOnChange(GXDLMSObject sender, bool dirty, int attributeIndex, object value)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new ObjectChangeEventHandler(DLMSItemOnChange), sender, dirty, attributeIndex, value);
            }
            else
            {
                if (SelectedView != null && SelectedView.Target == sender)
                {
                    UpdateDirty(SelectedView, ((Form)SelectedView).Controls, sender, attributeIndex, dirty);
                }
                TreeNode node = ObjectTreeItems[sender] as TreeNode;
                if (node != null)
                {
                    if (sender is GXDLMSProfileGeneric)
                    {
                        node.ImageIndex = node.SelectedImageIndex = 8;
                    }
                    else
                    {
                        node.ImageIndex = node.SelectedImageIndex = dirty ? 10 : 7;
                    }
                }
                UpdateWriteEnabled();
            }
        }

        /// <summary>
        /// User is executing action.
        /// </summary>
        private void OnAction(object sender, GXAsyncWork work, object[] parameters)
        {
            ClearTrace();
            UpdateTransaction(true);
            GXButton btn = parameters[0] as GXButton;
            GXDLMSMeter m = btn.Target.Parent.Tag as GXDLMSMeter;
            GXDLMSDevice dev = m as GXDLMSDevice;
            GXActionArgs ve = new GXActionArgs(btn.Target, btn.Index);
            if (dev != null)
            {
                ve.Client = dev.Comm.client;
                dev.KeepAliveStop();
            }
            ve.Action = btn.Action;
            GXDlmsUi.UpdateAccessRights(btn.View, btn.Target, false);
            try
            {
                do
                {
                    btn.View.PreAction(ve);
                    if (ve.Exception != null)
                    {
                        throw ve.Exception;
                    }
                    if (ve.Handled)
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            GXReplyData reply = new GXReplyData();
                            if (ve.Value is byte[][])
                            {
                                if (dev == null)
                                {
                                    List<KeyValuePair<GXDLMSObject, byte>> list = new List<KeyValuePair<GXDLMSObject, byte>>();
                                    list.Add(new KeyValuePair<GXDLMSObject, byte>(ve.Target, (byte)ve.Index));
                                    activeDC.ReadObjects(new GXDLMSMeter[] { m }, list);
                                }
                                else
                                {
                                    int pos = 0, cnt = ((byte[][])ve.Value).Length;
                                    foreach (byte[] it in (byte[][])ve.Value)
                                    {
                                        if (cnt != 1)
                                        {
                                            OnProgress(null, ve.Text, ++pos, cnt);
                                        }
                                        reply.Clear();
                                        dev.Comm.ReadDataBlock(it, ve.Text, 1, reply);
                                    }
                                }
                            }
                            else if (ve.Action == ActionType.Read)
                            {
                                if (dev == null)
                                {
                                    List<KeyValuePair<GXDLMSObject, byte>> list = new List<KeyValuePair<GXDLMSObject, byte>>();
                                    list.Add(new KeyValuePair<GXDLMSObject, byte>(ve.Target, (byte)ve.Index));
                                    activeDC.ReadObjects(new GXDLMSMeter[] { m }, list);
                                }
                                else
                                {
                                    //Is reading allowed.
                                    if ((ve.Target.GetAccess(ve.Index) & AccessMode.Read) != 0)
                                    {
                                        dev.Comm.ReadValue(ve.Target, ve.Index);
                                    }
                                }
                            }
                            else if (ve.Action == ActionType.Write)
                            {
                                if (dev == null)
                                {
                                    List<KeyValuePair<GXDLMSObject, byte>> list = new List<KeyValuePair<GXDLMSObject, byte>>();
                                    list.Add(new KeyValuePair<GXDLMSObject, byte>(ve.Target, (byte)ve.Index));
                                    activeDC.WriteObjects(new GXDLMSMeter[] { m }, list);
                                }
                                else
                                {
                                    //Is writing allowed.
                                    if ((ve.Target.GetAccess(ve.Index) & AccessMode.Read) != 0)
                                    {
                                        dev.Comm.Write(ve.Target, ve.Index);
                                        //Update UI.
                                        if (SelectedView != null && SelectedView.Target == ve.Target)
                                        {
                                            GXDlmsUi.UpdateProperty(ve.Target, ve.Index, SelectedView, true, false);
                                        }
                                    }
                                }
                            }
                            else if (ve.Action == ActionType.Action)
                            {
                                if (dev == null)
                                {
                                    List<KeyValuePair<GXDLMSObject, byte>> list = new List<KeyValuePair<GXDLMSObject, byte>>();
                                    list.Add(new KeyValuePair<GXDLMSObject, byte>(ve.Target, (byte)ve.Index));
                                    activeDC.MethodObjects(new GXDLMSMeter[] { m }, list);
                                }
                                else
                                {
                                    //Is action allowed.
                                    if (ve.Index < 0 || ve.Target.GetMethodAccess(ve.Index) != MethodAccessMode.NoAccess)
                                    {
                                        dev.Comm.MethodRequest(ve.Target, ve.Index, ve.Value, ve.Text, reply);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ve.Exception = ex;
                        }
                        btn.View.PostAction(ve);
                        if (ve.Exception != null)
                        {
                            throw ve.Exception;
                        }
                        ve.Value = null;
                    }
                } while (ve.Action != ActionType.None);
                ShowLastErrors(ve.Target as GXDLMSObject);
            }
            finally
            {
                if (ve.Rebooting)
                {
                    try
                    {
                        if (dev != null)
                        {
                            dev.Media.Close();
                        }
                    }
                    catch (Exception)
                    {
                        //It's ok if this fails.
                    }
                    OnStatusChanged(null, DeviceState.Initialized);
                    GXDlmsUi.ObjectChanged(SelectedView, btn.Target, false);
                }
                else
                {
                    if (dev != null)
                    {
                        dev.KeepAliveStart();
                        GXDlmsUi.UpdateAccessRights(btn.View, btn.Target, (dev.Status & DeviceState.Connected) != 0);
                    }
                    else
                    {
                        GXDlmsUi.UpdateAccessRights(btn.View, btn.Target, true);
                    }
                    UpdateTransaction(false);
                }
            }
        }

        void OnHandleAction(object sender, EventArgs e)
        {
            try
            {
                if (TransactionWork != null && TransactionWork.IsRunning)
                {
                    throw new Exception("Transaction in progress.");
                }
                TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, OnAction, OnError, null, new object[] { sender });
                TransactionWork.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        delegate void ValueChangedEventHandler(IGXDLMSView view, int index, object value, bool changeByUser, bool connected);

        void OnValueChanged(IGXDLMSView view, int index, object value, bool changeByUser, bool connected)
        {
            try
            {
                view.OnValueChanged(index, value, changeByUser, connected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        /// <summary>
        /// Create new device list.
        /// </summary>
        private void NewMnu_Click(object sender, EventArgs e)
        {
            try
            {
                //Save changes?
                if (this.Dirty)
                {
                    DialogResult ret = MessageBox.Show(this, Properties.Resources.SaveChangesTxt, Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ret == DialogResult.Cancel)
                    {
                        return;
                    }
                    if (ret == DialogResult.Yes)
                    {
                        if (!Save())
                        {
                            return;
                        }
                    }
                }
                if (TransactionWork != null)
                {
                    TransactionWork.Cancel();
                    TransactionWork = null;
                }
                path = "";
                ObjectTreeItems.Clear();
                SelectedListItems.Clear();
                ObjectListItems.Clear();
                ObjectValueItems.Clear();
                foreach (GXDLMSMeter it in Devices)
                {
                    if (it is GXDLMSDevice)
                    {
                        ((GXDLMSDevice)it).Disconnect();
                    }
                }

                /*--------------------- ici la la partie qui permet de faire ajouter plusieur compteur----------------------*/

                //----------------------DEBUT DE LA PARTIE MODIFIER-------------------------------------
                // Devices.Clear();

                ObjectTree.Nodes.Clear();
                // DeviceListViewItems.Clear();
                DeviceList.Items.Clear();
                // ObjectList.Items.Clear();
                // ObjectList.Groups.Clear();
                //-------------------------FIN DE LA PARTIE MODIFIER------------------------------------------------------------------

                ObjectValueView.Items.Clear();
                TreeNode node = ObjectTree.Nodes.Add("Devices");

                node.Tag = Devices;

                node.SelectedImageIndex = node.ImageIndex = 0;
                ObjectTree.SelectedNode = node;
                ConformanceTests.Items.Clear();
                SetDirty(false);






            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        GXDLMSMeter GetSelectedDevice()
        {
            if (this.ObjectTree.SelectedNode != null)
            {
                if (this.ObjectTree.SelectedNode.Tag is GXDLMSDeviceCollection)
                {
                    //If device list is selected.
                    return null;
                }
                else if (this.ObjectTree.SelectedNode.Tag is GXDLMSMeter)
                {
                    return this.ObjectTree.SelectedNode.Tag as GXDLMSMeter;
                }
                else if (this.ObjectTree.SelectedNode.Tag is GXDLMSObject)
                {
                    return ((GXDLMSObject)this.ObjectTree.SelectedNode.Tag).Parent.Tag as GXDLMSMeter;
                }
                else if (this.ObjectTree.SelectedNode.Parent != null)
                {
                    return (GXDLMSMeter)this.ObjectTree.SelectedNode.Parent.Tag;
                }
            }
            return null;
        }

        private DialogResult ShowProperties(GXDLMSMeter dev, bool newDevice)
        {
            string man = dev.Manufacturer;
            DevicePropertiesForm dlg = new DevicePropertiesForm(this.Manufacturers, dev);
            DialogResult res = dlg.ShowDialog(this);
            if (res == DialogResult.OK)
            {
                if (!newDevice)
                {
                    //If user has change meter manufacturer.
                    if (man != dev.Manufacturer)
                    {
                        while (dev.Objects.Count != 0)
                        {
                            RemoveObject(dev.Objects[0]);
                        }
                        SelectItem(dev);
                    }
                    ((TreeNode)ObjectTreeItems[dev]).Text = dev.Name;
                    SetDirty(true);
                }
            }
            return res;
        }

        /// <summary>
        /// Show properties of the selected media.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertiesMnu_Click(object sender, EventArgs e)
        {
            try
            {
                GXDLMSMeter dev = GetSelectedDevice();
                if (dev != null)
                {
                    ShowProperties(dev, false);
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Toggle toolbar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewToolbarMnu_Click(object sender, EventArgs e)
        {
            try
            {
                ViewToolbarMnu.Checked = !ViewToolbarMnu.Checked;
                toolStrip1.Visible = ViewToolbarMnu.Checked;
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Toggle statusbar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewStatusbarMnu_Click(object sender, EventArgs e)
        {
            try
            {
                ViewStatusbarMnu.Checked = !ViewStatusbarMnu.Checked;
                statusStrip1.Visible = ViewStatusbarMnu.Checked;
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Show About dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AboutMnu_Click(object sender, EventArgs e)
        {
            try
            {
                GXAboutForm lic = new GXAboutForm();
                lic.Title = GXDLMSDirector.Properties.Resources.AboutGXDLMSDirectorTxt;
                lic.Application = GXDLMSDirector.Properties.Resources.GXDLMSDirectorTxt;
                lic.AboutText = GXDLMSDirector.Properties.Resources.ForMoreInfoTxt;
                lic.CopyrightText = GXDLMSDirector.Properties.Resources.CopyrightTxt;
                //Get version info
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
                lic.ShowAbout(this, info.FileVersion);
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private static void Save(string path, GXDLMSMeterCollection devices)
        {
            using (Stream stream = File.Open(path, FileMode.Create))
            {
                GXFileSystemSecurity.UpdateFileSecurity(path);
                List<Type> types = new List<Type>(GXDLMSClient.GetObjectTypes());
                types.Add(typeof(GXDLMSAttributeSettings));
                types.Add(typeof(GXDLMSAttribute));
                using (TextWriter writer = new StreamWriter(stream))
                {
                    XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                    XmlAttributes attribs = new XmlAttributes();
                    attribs.XmlIgnore = true;
                    overrides.Add(typeof(GXDLMSDevice), "ObsoleteObjects", attribs);
                    overrides.Add(typeof(GXDLMSAttributeSettings), attribs);
                    GXDLMSDeviceCollection tmp = new GXDLMSDeviceCollection();
                    foreach (GXDLMSMeter it in devices)
                    {
                        tmp.Add(it as GXDLMSDevice);
                    }
                    XmlSerializer x = new XmlSerializer(typeof(GXDLMSDeviceCollection), overrides, types.ToArray(), null, "Gurux1");
                    x.Serialize(writer, tmp);
                    writer.Close();
                }
                stream.Close();
            }
        }

        private void SaveFile(string path)
        {
            Save(path, Devices);
            SetDirty(false);
            mruManager.Insert(0, path);
        }

        private bool Save()
        {
            if (string.IsNullOrEmpty(path))
            {
                return SaveAs();
            }
            SaveFile(path);
            return true;
        }

        private bool SaveAs()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterTxt;
            dlg.DefaultExt = ".gxc";
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                this.path = dlg.FileName;
                SaveFile(dlg.FileName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Save media settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveMnu_Click(object sender, EventArgs e)
        {
            try
            {
                Save();
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void SaveAsMnu_Click(object sender, EventArgs e)
        {
            try
            {
                SaveAs();
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        delegate void UpdateConformance(GXDLMSDevice device);

        void OnUpdateConformance(GXDLMSDevice device)
        {
            NegotiatedConformanceTB.Text = device.Comm.client.NegotiatedConformance.ToString();

        }

        void OnError(object sender, Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

        /// <summary>
        /// Ask is association view read when connecting first time.
        /// </summary>
        /// <param name="device"></param>
        delegate void FirstConnectionEventHandler(GXDLMSDevice device);

        /// <summary>
        /// Ask is association view read when connecting first time.
        /// </summary>
        /// <param name="device">Connected meter.</param>
        private void OnFirstConnection(GXDLMSDevice device)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new FirstConnectionEventHandler(OnFirstConnection), device);
            }
            else
            {
                if (MessageBox.Show(this, Properties.Resources.ReadAssociationView, Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Refresh, OnError, null, new object[] { device });
                }
                TransactionWork.Start();
            }
        }

        void Connect(object sender, GXAsyncWork work, object[] parameters)
        {
            try
            {
                object obj = parameters[0];
                int pos = 0;
                int cnt;
                if (obj is GXDLMSMeterCollection)
                {
                }
                else if (obj is GXDLMSDeviceCollection)
                {
                    cnt = ((GXDLMSDeviceCollection)obj).Count;
                    foreach (GXDLMSDevice it in (GXDLMSDeviceCollection)obj)
                    {
                        if (!it.Media.IsOpen)
                        {
                            this.OnProgress(null, "Connecting", ++pos, cnt);
                            it.InitializeConnection();
                        }
                    }
                }
                else if (obj is GXDLMSDevice)
                {
                    if (!((GXDLMSDevice)obj).Media.IsOpen)
                    {
                        GXDLMSDevice dev = (GXDLMSDevice)obj;
                        this.OnProgress(null, "Connecting", 0, 1);
                        dev.InitializeConnection();
                        if (InvokeRequired)
                        {
                            BeginInvoke(new UpdateConformance(this.OnUpdateConformance), (GXDLMSDevice)obj);
                        }
                        if (dev.Objects.Count == 0)
                        {
                            OnFirstConnection(dev);
                        }
                        if (dev.PreEstablished)
                        {
                            traceTranslator.ServerSystemTitle = GXCommon.HexToBytes(dev.ServerSystemTitle);
                        }
                        else
                        {
                            traceTranslator.ServerSystemTitle = dev.Comm.client.ServerSystemTitle;
                        }
                        traceTranslator.DedicatedKey = dev.Comm.client.Ciphering.DedicatedKey;
                    }
                }
                else if (obj is GXDLMSObjectCollection)
                {
                    GXDLMSObjectCollection tmp = obj as GXDLMSObjectCollection;
                    GXDLMSDeviceCollection devices = new GXDLMSDeviceCollection();
                    foreach (GXDLMSObject it in tmp)
                    {
                        GXDLMSDevice dev = it.Parent.Tag as GXDLMSDevice;
                        if (!devices.Contains(dev))
                        {
                            devices.Add(dev);
                        }
                    }
                    Connect(sender, work, new object[] { devices });
                    if (tmp.Count == 1)
                    {
                        GXDlmsUi.ObjectChanged(SelectedView, tmp[0] as GXDLMSObject, true);
                    }
                }
                else
                {
                    this.OnProgress(null, "Connecting", 0, 1);
                    GXDLMSObject tmp = obj as GXDLMSObject;
                    GXDLMSDevice dev = tmp.Parent.Tag as GXDLMSDevice;
                    dev.InitializeConnection();
                    if (dev.PreEstablished)
                    {
                        traceTranslator.ServerSystemTitle = GXCommon.HexToBytes(dev.ServerSystemTitle);
                    }
                    else
                    {
                        traceTranslator.ServerSystemTitle = dev.Comm.client.ServerSystemTitle;
                    }
                    traceTranslator.DedicatedKey = dev.Comm.client.Ciphering.DedicatedKey;
                    GXDlmsUi.ObjectChanged(SelectedView, tmp as GXDLMSObject, true);
                }
            }
            catch (ThreadAbortException)
            {
                //User has cancel action. Do nothing.
            }
            catch (Exception Ex)
            {
              //  textBox1.Text= "Failed to receive reply from the device in given time.";

                if (!messeageErreur && Ex.Message.Equals("Failed to receive reply from the device in given time."))
                {
                    // GXDLMS.Common.Error.ShowError(sender as Form, Ex);
                }
                else
                {
                    GXDLMS.Common.Error.ShowError(sender as Form, Ex);
                }

               
            }
            finally
            {
                this.OnProgress(null, "Connecting", 1, 1);
            }
        }

        /// <summary>
        /// Connect to the selected device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectMnu_Click(object sender, EventArgs e)
        {
            // textBox1.Text = "test de connexion";
            ClearTrace();
            if (tabControl1.SelectedIndex == 0)
            {
                //   textBox1.Text = "test de connexion if";
                TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Connect, OnError, null, new object[] { ObjectTree.SelectedNode.Tag });
            }
            else
            {
                //textBox1.Text = "test de connexion else";
                TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Connect, OnError, null, new object[] { GetDevices() });
            }
            TransactionWork.Start();
        }

        void Disconnect(object sender, GXAsyncWork work, object[] parameters)
        {
            try
            {
                ClearTrace();
                int pos = 0;
                int cnt;
                object obj = parameters[0];
                if (obj is GXDLMSDeviceCollection)
                {
                    cnt = ((GXDLMSDeviceCollection)obj).Count;
                    foreach (GXDLMSDevice it in (GXDLMSDeviceCollection)obj)
                    {
                        (sender as Form).BeginInvoke(new ProgressEventHandler(this.OnProgress), null, "Disconnecting", ++pos, cnt);
                        it.Disconnect();
                    }
                }
                else if (obj is GXDLMSDevice)
                {
                    (sender as Form).BeginInvoke(new ProgressEventHandler(this.OnProgress), null, "Disconnecting", 0, 1);
                    ((GXDLMSDevice)obj).Disconnect();
                }
                else if (obj is GXDLMSObjectCollection)
                {
                    GXDLMSObjectCollection tmp = obj as GXDLMSObjectCollection;
                    GXDLMSDeviceCollection devices = new GXDLMSDeviceCollection();
                    foreach (GXDLMSObject it in tmp)
                    {
                        GXDLMSDevice dev = it.Parent.Tag as GXDLMSDevice;
                        if (!devices.Contains(dev))
                        {
                            devices.Add(dev);
                        }
                    }
                    Disconnect(sender, work, new object[] { devices });
                    if (tmp.Count == 1)
                    {
                        GXDlmsUi.ObjectChanged(SelectedView, tmp[0] as GXDLMSObject, false);
                    }
                }
                else
                {
                    (sender as Form).BeginInvoke(new ProgressEventHandler(OnProgress), sender, "Disconnecting", 0, 1);
                    GXDLMSObject tmp = obj as GXDLMSObject;
                    GXDLMSDevice dev = tmp.Parent.Tag as GXDLMSDevice;
                    dev.Disconnect();
                    GXDlmsUi.ObjectChanged(SelectedView, tmp, false);
                }
            }
            catch (ThreadAbortException)
            {
                //User has cancel action. Do nothing.
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError((sender as Form), Ex);
            }
            finally
            {
                (sender as Form).BeginInvoke(new ProgressEventHandler(this.OnProgress), sender, "", 1, 1);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        GXDLMSMeterCollection GetDevices()
        {
            GXDLMSMeterCollection devices = new GXDLMSMeterCollection();
            foreach (GXDLMSObject it in SelectedListItems.Values)
            {
                GXDLMSDevice dev = it.Parent.Tag as GXDLMSDevice;
                if (!devices.Contains(dev))
                {
                    devices.Add(dev);
                }
            }
            //If no device item is selected return all devices.
            if (devices.Count == 0)
            {
                return this.Devices;
            }
            return devices;
        }

        /// <summary>
        /// Disconnect to the selected device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisconnectMnu_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Disconnect, OnError, null, new object[] { ObjectTree.SelectedNode.Tag });
            }
            else
            {
                TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Disconnect, OnError, null, new object[] { GetDevices() });
            }
            TransactionWork.Start();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                events.Close();
                //Save changes?
                if (this.Dirty)
                {
                    DialogResult ret = MessageBox.Show(this, Properties.Resources.SaveChangesTxt, Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ret == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                    }
                    if (ret == DialogResult.Yes)
                    {
                        if (!Save())
                        {
                            e.Cancel = true;
                        }
                    }
                }
                if (e.Cancel)
                {
                    return;
                }
                if (addInForms != null)
                {
                    foreach (Form it in addInForms)
                    {
                        it.Close();
                        it.Dispose();
                    }
                    addInForms = null;
                }
                SaveXmlPositioning();
                SetDirty(false);
                Manufacturers.WriteManufacturerSettings();
                foreach (GXDLMSMeter it in Devices)
                {
                    if (it is GXDLMSDevice)
                    {
                        (it as GXDLMSDevice).Disconnect();
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        #region XML positioning

        /// <summary>
        /// Retrieves application data path from environment variables.
        /// </summary>
        public static string UserDataPath
        {
            get
            {
                string path;
                if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                }
                else
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }
                return path;
            }
        }

        /// <summary>
        /// Save window position to a xml-file
        /// </summary>
        private void SaveXmlPositioning()
        {
            try
            {
                Properties.Settings.Default.ViewToolbar = ViewToolbarMnu.Checked;
                Properties.Settings.Default.ViewStatusbar = ViewStatusbarMnu.Checked;
                Properties.Settings.Default.ViewTree = ObjectTreeMnu.Checked;
                Properties.Settings.Default.ViewList = ObjectListMnu.Checked;
                Properties.Settings.Default.ViewGroups = GroupsMnu.Checked;
                Properties.Settings.Default.TraceType = traceLevel;
                Properties.Settings.Default.NotificationType = notificationLevel;
                Properties.Settings.Default.ForceRead = ForceReadMnu.Checked;
                Properties.Settings.Default.NotificationAutoReset = AutoReset.Checked;

                Properties.Settings.Default.LogComments = LogCommentsMenu.Checked;
                Properties.Settings.Default.TraceComments = TraceCommentsMenu.Checked;
                Properties.Settings.Default.NotificationsComments = NotificationsCommentsMenu.Checked;

                if (EventsView.Height > 50)
                {
                    Properties.Settings.Default.EventsViewHeight = EventsView.Height;
                }
                if (TraceView.Height > 50)
                {
                    Properties.Settings.Default.TraceViewHeight = TraceView.Height;
                }
                Properties.Settings.Default.Save();
                GXDlmsUi.Save();

                string path = UserDataPath;
                if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    path = Path.Combine(path, ".Gurux");
                }
                else
                {
                    path = Path.Combine(path, "Gurux");
                }
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, "GXDLMSDirector.xml");
                using (XmlTextWriter xtw = new XmlTextWriter(File.CreateText(path)))
                {
                    xtw.WriteStartDocument();
                    xtw.WriteStartElement("GXDLMSDirector");
                    xtw.WriteStartElement("MRU");
                    foreach (string it in mruManager.GetNames())
                    {
                        xtw.WriteElementString("item", it);
                    }
                    xtw.WriteEndElement();
                    xtw.WriteStartElement("MainWindow");
                    xtw.WriteAttributeString("State", ((int)this.WindowState).ToString());
                    if (this.WindowState == FormWindowState.Normal)
                    {
                        xtw.WriteAttributeString("X", this.Bounds.X.ToString());
                        xtw.WriteAttributeString("Y", this.Bounds.Y.ToString());
                        xtw.WriteAttributeString("Width", this.Bounds.Width.ToString());
                        xtw.WriteAttributeString("Height", this.Bounds.Height.ToString());
                    }
                    xtw.WriteEndElement();
                    xtw.WriteStartElement("Views");
                    Rectangle rc = this.tabControl1.Bounds;
                    xtw.WriteAttributeString("X", rc.X.ToString());
                    xtw.WriteAttributeString("Y", rc.Y.ToString());
                    xtw.WriteAttributeString("Width", rc.Width.ToString());
                    xtw.WriteAttributeString("Height", rc.Height.ToString());
                    xtw.WriteEndElement();
                    xtw.WriteEndDocument();
                    xtw.Close();
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Loads window positioning from a xml-file.
        /// </summary>
        private void LoadXmlPositioning()
        {
            try
            {
                ViewToolbarMnu.Checked = !Properties.Settings.Default.ViewToolbar;
                ViewToolbarMnu_Click(null, null);
                ViewStatusbarMnu.Checked = !Properties.Settings.Default.ViewStatusbar;
                ViewStatusbarMnu_Click(null, null);
                ObjectTreeMnu.Checked = !Properties.Settings.Default.ViewTree;
                ObjectTreeMnu_Click(null, null);
                ObjectListMnu.Checked = !Properties.Settings.Default.ViewList;
                ObjectListMnu_Click(null, null);
                GroupsMnu.Checked = !Properties.Settings.Default.ViewGroups;
                GroupsMnu_Click(null, null);
                if (Properties.Settings.Default.TraceType == 0)
                {
                    noneToolStripMenuItem_Click(null, null);
                }
                else if (Properties.Settings.Default.TraceType == 1)
                {
                    hexToolStripMenuItem_Click(null, null);
                }
                else if (Properties.Settings.Default.TraceType == 2)
                {
                    xmlToolStripMenuItem_Click(null, null);
                }
                else if (Properties.Settings.Default.TraceType == 3)
                {
                    pDUToolStripMenuItem_Click(null, null);
                }

                if (Properties.Settings.Default.NotificationType == 0)
                {
                    NotificationNone_Click(null, null);
                }
                else if (Properties.Settings.Default.NotificationType == 1)
                {
                    NotificationHex_Click(null, null);
                }
                else if (Properties.Settings.Default.NotificationType == 2)
                {
                    NotificationXml_Click(null, null);
                }
                else if (Properties.Settings.Default.NotificationType == 3)
                {
                    NotificationPdu_Click(null, null);
                }

                LogTimeMnu.Checked = !Properties.Settings.Default.LogTime;
                LogTimeMnu_Click(null, null);
                LogDuration.Checked = !Properties.Settings.Default.LogDuration;
                LogDuration_Click(null, null);

                TraceTimeMnu.Checked = !Properties.Settings.Default.TraceTime;
                TraceTimeMnu_Click(null, null);
                TraceDuration.Checked = !Properties.Settings.Default.TraceDuration;
                TraceDuration_Click(null, null);

                NotificationTimeMnu.Checked = !Properties.Settings.Default.NotificationTime;
                NotificationTimeMnu_Click(null, null);

                conformanceTestsToolStripMenuItem1.Checked = !Properties.Settings.Default.CTT;
                conformanceTestsToolStripMenuItem1_Click(null, null);


                TraceView.Height = Properties.Settings.Default.TraceViewHeight;
                EventsView.Height = Properties.Settings.Default.EventsViewHeight;

                ForceReadMnu.Checked = !Properties.Settings.Default.ForceRead;
                ForceReadMnu_Click(null, null);
                AutoReset.Checked = !Properties.Settings.Default.NotificationAutoReset;
                AutoReset_Click(null, null);

                LogCommentsMenu.Checked = !Properties.Settings.Default.LogComments;
                LogCommentsMenu_Click(null, null);
                TraceCommentsMenu.Checked = !Properties.Settings.Default.TraceComments;
                TraceCommentsMenu_Click(null, null);
                NotificationsCommentsMenu.Checked = !Properties.Settings.Default.NotificationsComments;
                NotificationsCommentsMenu_Click(null, null);

                LogHexBtn.Checked = (Properties.Settings.Default.Log & 1) != 0;
                LogXmlBtn.Checked = (Properties.Settings.Default.Log & 2) != 0;
                GXLogWriter.LogLevel = Properties.Settings.Default.Log;

                string path = UserDataPath;
                if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    path = Path.Combine(path, ".Gurux");
                }
                else
                {
                    path = Path.Combine(path, "Gurux");
                }
                path = Path.Combine(path, "GXDLMSDirector.xml");
                if (!File.Exists(path))
                {
                    return;
                }
                using (XmlTextReader xtr = new XmlTextReader(File.OpenText(path)))
                {
                    while (xtr.Read())
                    {
                        if (xtr.Name == "General")
                        {
                        }
                        else if (xtr.Name == "MRU" && !xtr.IsEmptyElement)
                        {
                            xtr.Read();
                            while (xtr.Name != "MRU" && xtr.NodeType != XmlNodeType.EndElement)
                            {
                                mruManager.Insert(-1, xtr.ReadElementString());
                            }
                        }
                        else if (xtr.Name == "MainWindow")
                        {
                            string val = xtr.GetAttribute("State");
                            if (val != null)
                            {
                                FormWindowState state = (FormWindowState)int.Parse(val);
                                if (state == FormWindowState.Normal)
                                {
                                    this.SetBoundsCore(int.Parse(xtr.GetAttribute("X")),
                                                       int.Parse(xtr.GetAttribute("Y")),
                                                       int.Parse(xtr.GetAttribute("Width")),
                                                       int.Parse(xtr.GetAttribute("Height")), BoundsSpecified.All);
                                }
                                //If dual display and other display is removed.
                                Rectangle rc = Screen.GetWorkingArea(this);
                                if (this.Left > rc.Width)
                                {
                                    this.Left = 0;
                                }
                                if (this.Top > rc.Height)
                                {
                                    this.Top = 0;
                                }
                            }
                        }
                        else if (xtr.Name == "Views")
                        {
                            tabControl1.SetBounds(int.Parse(xtr.GetAttribute("X")),
                                                  int.Parse(xtr.GetAttribute("Y")),
                                                  int.Parse(xtr.GetAttribute("Width")),
                                                  int.Parse(xtr.GetAttribute("Height")));
                        }
                    }
                    xtr.Close();
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        #endregion

        void ReadDevices()
        {
            foreach (GXDLMSMeter it in Devices)
            {
                ReadDevice(it);
            }
        }

        void ReadDevice(GXDLMSMeter d)
        {
            GXDLMSDevice dev = d as GXDLMSDevice;
            //If DC.
            if (dev == null)
            {
                int cnt = d.Objects.Count;
                if (cnt == 0)
                {
                    RefreshDevice(d, true);
                    //Do not try to refresh and read values at the same time.
                    return;
                }
                foreach (GXDLMSObject obj in d.Objects)
                {
                    List<KeyValuePair<GXDLMSObject, byte>> list = new List<KeyValuePair<GXDLMSObject, byte>>();
                    int[] indexes = (obj as IGXDLMSBase).GetAttributeIndexToRead(ForceRefreshBtn.Checked);
                    foreach (byte pos in indexes)
                    {
                        OnBeforeRead(obj, pos, null);
                        list.Add(new KeyValuePair<GXDLMSObject, byte>(obj, pos));
                    }
                    OnBeforeRead(obj, 0, null);
                    activeDC.ReadObjects(new GXDLMSMeter[] { obj.Parent.Tag as GXDLMSMeter }, list);
                    foreach (byte pos in indexes)
                    {
                        OnAfterRead(obj, pos, null);
                    }
                    DLMSItemOnChange(obj, false, 0, null);
                }
            }
            else
            {
                try
                {
                    dev.Comm.OnBeforeRead += new ReadEventHandler(OnBeforeRead);
                    dev.Comm.OnAfterRead += new ReadEventHandler(OnAfterRead);
                    dev.KeepAliveStop();
                    int cnt = dev.Objects.Count;
                    if (cnt == 0)
                    {
                        RefreshDevice(dev, true);
                        //Do not try to refresh and read values at the same time.
                        return;
                    }
                    int pos = 0;
                    foreach (GXDLMSObject it in dev.Objects)
                    {
                        OnProgress(dev, "Reading " + it.LogicalName + "...", ++pos, cnt);
                        dev.Comm.Read(this, it, ForceRefreshBtn.Checked);
                        DLMSItemOnChange(it, false, 0, null);
                    }
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
                finally
                {
                    dev.Comm.OnBeforeRead -= new ReadEventHandler(OnBeforeRead);
                    dev.Comm.OnAfterRead -= new ReadEventHandler(OnAfterRead);
                    dev.KeepAliveStart();
                }
            }
        }

        delegate void UpdateTransactionEventHandler(bool start);

        void OnTrace(DateTime time, GXDLMSDevice sender, string trace, byte[] data, int length, string path, int duration)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MessageTraceEventHandler(this.OnTrace), time, sender, trace, data, length, path, duration);
            }
            else
            {
                //Show data as hex.
                if (traceLevel == 1)
                {
                    if (TraceTimeMnu.Checked)
                    {
                        TraceView.AppendText(time.ToString("HH:mm:ss") + " " + trace + " " + GXCommon.ToHex(data, true) + Environment.NewLine);
                    }
                    else
                    {
                        TraceView.AppendText(trace + " " + GXCommon.ToHex(data, true) + Environment.NewLine);
                    }
                }
                else if (data != null && (traceLevel == 2 || traceLevel == 3))
                {
                    //Show data as xml or pdu.
                    receivedTraceData.Set(data);
                    try
                    {
                        GXByteBuffer pdu = new GXByteBuffer();
                        InterfaceType type = GXDLMSTranslator.GetDlmsFraming(receivedTraceData);
                        while (traceTranslator.FindNextFrame(receivedTraceData, pdu, type))
                        {
                            if (TraceTimeMnu.Checked)
                            {
                                TraceView.AppendText(Environment.NewLine + time.ToString("HH:mm:ss") + Environment.NewLine + traceTranslator.MessageToXml(receivedTraceData));
                            }
                            else
                            {
                                TraceView.AppendText(Environment.NewLine + traceTranslator.MessageToXml(receivedTraceData));
                            }
                            receivedTraceData.Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        receivedTraceData.Clear();
                        TraceView.ResetText();
                        TraceView.AppendText(Environment.NewLine + time.ToString() + ex.Message + Environment.NewLine);
                        TraceView.AppendText(trace + Environment.NewLine + GXCommon.ToHex(data, true) + Environment.NewLine);
                    }
                }
                if (duration != 0)
                {
                    if (Properties.Settings.Default.TraceDuration)
                    {
                        TraceView.AppendText("Duration: " + duration.ToString() + Environment.NewLine);
                    }
                }
            }
        }

        void UpdateTransaction(bool start)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new UpdateTransactionEventHandler(this.UpdateTransaction), start);
            }
            else
            {
                ReadBtn.Enabled = ReadMnu.Enabled = !start;
                if (!start)
                {
                    UpdateWriteEnabled();
                }
                else
                {
                    WriteBtn.Enabled = WriteMnu.Enabled = !start;
                }
            }
        }

        public void OnBeforeRead(GXDLMSObject sender, int index, Exception ex)
        {
            try
            {
                if ((index == 2 || index == 3) && !this.IsDisposed && sender is GXDLMSProfileGeneric)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new ReadEventHandler(OnBeforeRead), new object[] { sender, index, ex });
                    }
                    else if (SelectedView != null && SelectedView.Target == sender)
                    {
                        GXDLMSProfileGeneric pg = sender as GXDLMSProfileGeneric;
                        pg.Buffer.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        void UpdateValue(GXDLMSObject sender, int index, ListViewItem lv)
        {
            if (index != 1)
            {
                object value = sender.GetValues()[index - 1];
                string str;
                if (value != null && value.GetType().IsArray)
                {
                    str = Convert.ToString(GXDLMS.Common.GXHelpers.ConvertFromDLMS(value, DataType.None, DataType.None, true, false));
                }
                else
                {
                    str = GXHelpers.ConvertDLMS2String(value);
                }
                lv.SubItems[index].Text = str;
            }
        }

        public void OnAfterRead(GXDLMSObject sender, int index, Exception ex)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new ReadEventHandler(OnAfterRead), new object[] { sender, index, ex });
                    return;
                }
                if (SelectedView != null && SelectedView.Target == sender)
                {
                    GXDlmsUi.UpdateProperty(sender as GXDLMSObject, index, SelectedView, true, false);
                }
                ListViewItem lv = ObjectValueItems[sender] as ListViewItem;
                if (lv != null)
                {
                    UpdateValue(sender, index, lv);

                    i++;
                    // textBox1.Text += (i / 2) + "   " + sender.LogicalName + "  " + sender.Description + " Valeur :   " + lv.SubItems[index].Text + "\n";

                    if (!lv.SubItems[index].Text.Equals("Data"))
                    {
                        textBox1.Text += (i / 2) + "   " + sender.LogicalName + "  " + sender.Description + " Valeur :   " + lv.SubItems[index].Text + "\n";
                     //BDUtils.insertion(sender.LogicalName, sender.Description, lv.SubItems[index].Text);

                    }


                }
                if (ex != null && SelectedView != null)
                {
                    GXDlmsUi.UpdateError(SelectedView, sender as GXDLMSObject, index, ex);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        void Read(object sender, GXAsyncWork work, object[] parameters)
        {
            GXDLMSDevice dev = null;
            try
            {
                ClearTrace();
                UpdateTransaction(true);
                object item = parameters[0];
                IGXDLMSView SelectedView = parameters[1] as IGXDLMSView;
                if (item != null)
                {
                    //Read all objects if device is selected.
                    if (item is GXDLMSMeterCollection)
                    {
                        if (activeDC != null && (item as GXDLMSMeterCollection).Count == 0)
                        {
                            Refresh(null, null, new object[] { item });
                        }
                        else
                        {
                            ReadDevices();
                        }
                    }
                    else if (item is GXDLMSMeter)
                    {
                        dev = item as GXDLMSDevice;
                        ReadDevice(item as GXDLMSMeter);
                    }
                    else if (item is GXDLMSObject)
                    {
                        GXDLMSObject obj = item as GXDLMSObject;
                        this.OnProgress(dev, "Reading...", 0, 1);
                        dev = obj.Parent.Tag as GXDLMSDevice;
                        //If DC...
                        if (dev == null)
                        {
                            List<KeyValuePair<GXDLMSObject, byte>> list = new List<KeyValuePair<GXDLMSObject, byte>>();
                            int[] indexes = (obj as IGXDLMSBase).GetAttributeIndexToRead(ForceRefreshBtn.Checked);
                            foreach (byte pos in indexes)
                            {
                                OnBeforeRead(obj, pos, null);
                                list.Add(new KeyValuePair<GXDLMSObject, byte>(obj, pos));
                            }
                            OnBeforeRead(obj, 0, null);
                            activeDC.ReadObjects(new GXDLMSMeter[] { obj.Parent.Tag as GXDLMSMeter }, list);
                            foreach (byte pos in indexes)
                            {
                                OnAfterRead(obj, pos, null);
                            }
                            DLMSItemOnChange(obj, false, 0, null);
                        }
                        else
                        {
                            dev.KeepAliveStop();
                            try
                            {
                                dev.Comm.OnBeforeRead += new ReadEventHandler(OnBeforeRead);
                                dev.Comm.OnAfterRead += new ReadEventHandler(OnAfterRead);
                                if (parameters.Length == 3 && (bool)parameters[2])
                                {
                                    List<KeyValuePair<GXDLMSObject, int>> list = new List<KeyValuePair<GXDLMSObject, int>>();
                                    foreach (int index in ((IGXDLMSBase)obj).GetAttributeIndexToRead(ForceRefreshBtn.Checked))
                                    {
                                        list.Add(new KeyValuePair<GXDLMSObject, int>(obj, index));
                                    }
                                    dev.Comm.ReadList(list);
                                }
                                else
                                {
                                    dev.Comm.Read(this, obj, ForceRefreshBtn.Checked);
                                    /*recuperation des valeur après lecture*/
                                    RecuperationData();
                                    RecuperationExtendedRegister();
                                    RecuperationRegister();
                                    RecuperationRegisterMonitor();
                                   
                                    /* fin de la recuperation après lecture */
                                    if (actifSauvegarde)
                                    {
                                        ReadInsertionBd();
                                       
                                    }
                                    ajouterBotton();
                                }
                            }
                            finally
                            {
                                dev.Comm.OnBeforeRead -= new ReadEventHandler(OnBeforeRead);
                                dev.Comm.OnAfterRead -= new ReadEventHandler(OnAfterRead);
                                if (dev.Comm.media.IsOpen)
                                {
                                    DLMSItemOnChange(obj, false, 0, null);
                                    dev.KeepAliveStart();
                                }
                            }
                            ShowLastErrors(obj);
                        }
                    }
                    else if (item is GXDLMSObjectCollection)
                    {
                        GXDLMSObjectCollection items = item as GXDLMSObjectCollection;
                        dev = items[0].Parent.Tag as GXDLMSDevice;
                        //If DC...
                        if (dev == null)
                        {
                            foreach (GXDLMSObject obj in items)
                            {
                                List<KeyValuePair<GXDLMSObject, byte>> list = new List<KeyValuePair<GXDLMSObject, byte>>();
                                int[] indexes = (obj as IGXDLMSBase).GetAttributeIndexToRead(ForceRefreshBtn.Checked);
                                foreach (byte pos in indexes)
                                {
                                    OnBeforeRead(obj, pos, null);
                                    list.Add(new KeyValuePair<GXDLMSObject, byte>(obj, pos));
                                }
                                OnBeforeRead(obj, 0, null);
                                activeDC.ReadObjects(new GXDLMSMeter[] { obj.Parent.Tag as GXDLMSMeter }, list);
                                foreach (byte pos in indexes)
                                {
                                    OnAfterRead(obj, pos, null);
                                }
                                DLMSItemOnChange(obj, false, 0, null);
                            }
                        }
                        else
                        {
                            dev.KeepAliveStop();
                            int pos = 0;
                            foreach (GXDLMSObject obj in items)
                            {
                                this.OnProgress(dev, "Reading...", pos++, items.Count);
                                try
                                {
                                    dev.Comm.OnBeforeRead += new ReadEventHandler(OnBeforeRead);
                                    dev.Comm.OnAfterRead += new ReadEventHandler(OnAfterRead);
                                    dev.Comm.Read(this, obj, ForceRefreshBtn.Checked);
                                }
                                finally
                                {
                                    dev.Comm.OnBeforeRead -= new ReadEventHandler(OnBeforeRead);
                                    dev.Comm.OnAfterRead -= new ReadEventHandler(OnAfterRead);
                                }
                                DLMSItemOnChange(obj, false, 0, null);
                            }
                            dev.KeepAliveStart();
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                //User has cancel action. Do nothing.
            }
            catch (GXDLMSException ex)
            {
                if (ex.ErrorCode == (int)ErrorCode.ReadWriteDenied ||
                        ex.ErrorCode == (int)ErrorCode.UndefinedObject ||
                        ex.ErrorCode == (int)ErrorCode.UnavailableObject ||
                        //Actaris returns access violation error.
                        ex.ErrorCode == (int)ErrorCode.AccessViolated)
                {
                    GXDLMS.Common.Error.ShowError(sender as Form, ex);
                }
                else
                {
                    GXDLMS.Common.Error.ShowError(sender as Form, ex);
                    if (dev != null)
                    {
                        dev.Disconnect();
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(sender as Form, Ex);
            }
            finally
            {
                if (dev == null)
                {
                    UpdateTransaction(false);
                }
                else
                {
                    if (!dev.Comm.media.IsOpen)
                    {
                        dev.Disconnect();
                    }
                    else if ((dev.Status & DeviceState.Connected) != 0)
                    {
                        UpdateTransaction(false);
                    }
                }
            }
            routineStatu(this.ObjectTree.SelectedNode);
        }

        /// <summary>
        /// Read selected DLMS object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadMnu_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                //Set focus to the tree or Read count do not updated.
                this.ObjectTree.Focus();
                if (this.ObjectTree.SelectedNode != null)
                {
                    TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Read, OnError, null, new object[] { this.ObjectTree.SelectedNode.Tag, SelectedView });
                    TransactionWork.Start();
                }
            }
            else
            {
                if (this.ObjectList.SelectedItems.Count != 0)
                {
                    GXDLMSObjectCollection items = new GXDLMSObjectCollection();
                    foreach (ListViewItem it in this.ObjectList.SelectedItems)
                    {
                        items.Add(it.Tag as GXDLMSObject);
                    }
                    TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Read, OnError, null, new object[] { items, SelectedView });
                    TransactionWork.Start();
                }
            }
        }

        private void WriteMnu_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateTransaction(true);
                if (this.ObjectTree.SelectedNode != null)
                {
                    //Read all objects if device is selected.
                    if (this.ObjectTree.SelectedNode.Tag is GXDLMSDeviceCollection)
                    {
                        throw new Exception("Only objects can be write, not whole devices.");
                    }
                    else if (this.ObjectTree.SelectedNode.Tag is GXDLMSDevice)
                    {
                        throw new Exception("Only objects can be write, not whole devices.");
                    }
                    else if (this.ObjectTree.SelectedNode.Tag is GXDLMSObjectCollection)
                    {
                        GXDLMSObjectCollection objects = (GXDLMSObjectCollection)this.ObjectTree.SelectedNode.Tag;
                        GXDLMSDevice dev = objects.Tag as GXDLMSDevice;
                        dev.Comm.OnError += new ReadEventHandler(OnError);
                        try
                        {
                            dev.KeepAliveStop();
                            OnProgress(dev, "Writing...", 0, 1);
                            foreach (GXDLMSObject obj in objects)
                            {
                                dev.Comm.Write(obj, 0);
                                GXDlmsUi.UpdateProperty(obj as GXDLMSObject, 0, SelectedView, true, false);
                                ShowLastErrors(obj);
                            }
                        }
                        finally
                        {
                            dev.Comm.OnError -= new ReadEventHandler(OnError);
                            dev.KeepAliveStart();
                        }
                    }
                    else if (this.ObjectTree.SelectedNode.Tag is GXDLMSObject)
                    {
                        GXDLMSObject obj = (GXDLMSObject)this.ObjectTree.SelectedNode.Tag;
                        if (activeDC != null)
                        {
                            object val;
                            List<KeyValuePair<GXDLMSObject, byte>> objects = new List<KeyValuePair<GXDLMSObject, byte>>();
                            for (int it = 1; it != (obj as IGXDLMSBase).GetAttributeCount() + 1; ++it)
                            {
                                if (obj.GetDirty(it, out val))
                                {
                                    objects.Add(new KeyValuePair<GXDLMSObject, byte>(obj, (byte)it));
                                }
                            }
                            if (objects.Count != 0)
                            {
                                activeDC.WriteObjects(new GXDLMSMeter[] { obj.Parent.Tag as GXDLMSMeter }, objects);
                            }
                        }
                        GXDLMSDevice dev = obj.Parent.Tag as GXDLMSDevice;
                        try
                        {
                            dev.KeepAliveStop();
                            dev.Comm.OnError += new ReadEventHandler(OnError);
                            OnProgress(dev, "Writing...", 0, 1);
                            dev.Comm.Write(obj, 0);
                            GXDlmsUi.UpdateProperty(obj as GXDLMSObject, 0, SelectedView, true, false);
                            ShowLastErrors(obj);
                        }
                        finally
                        {
                            dev.Comm.OnError -= new ReadEventHandler(OnError);
                            dev.KeepAliveStart();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GXDLMS.Common.Error.ShowError(this, ex);
            }
            finally
            {
                StatusLbl.Text = GetReadyText();
                UpdateTransaction(false);
            }
        }

        public void OnError(GXDLMSObject sender, int index, Exception ex)
        {
            GXDlmsUi.UpdateError(SelectedView, sender as GXDLMSObject, index, ex);
        }

        private string GetReadyText()
        {
            string str = Properties.Resources.ReadyTxt;
            if (activeDC != null)
            {
                str += " " + activeDC.Name;
            }
            return str;
        }

        /// <summary>
        /// Close application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitMnu_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void RemoveDevice(GXDLMSDevice dev)
        {
            dev.Disconnect();
            dev.OnProgress -= new ProgressEventHandler(this.OnProgress);
            dev.OnStatusChanged -= new StatusEventHandler(this.OnStatusChanged);
            Devices.Remove(dev);
            TreeNode node = (TreeNode)ObjectTreeItems[dev];
            node.Remove();
            ObjectTreeItems.Remove(dev);
            ListViewItem item = (ListViewItem)DeviceListViewItems[dev];
            DeviceList.Items.Remove(item);
            DeviceListViewItems.Remove(dev);
            while (dev.Objects.Count != 0)
            {
                RemoveObject(dev.Objects[0]);
            }
        }

        delegate void RemoveObjectEventHandler(object target);

        void RemoveObject(object target)
        {
            if (InvokeRequired)
            {
                Invoke(new RemoveObjectEventHandler(RemoveObject), target);
            }
            else
            {
                OnDirty(true);
                if (target is GXDLMSObject)
                {
                    GXDLMSObject obj = target as GXDLMSObject;
                    int pos = SelectedListItems.IndexOfValue(obj);
                    if (pos != -1)
                    {
                        SelectedListItems.RemoveAt(pos);
                    }
                    ListViewItem lv = ObjectValueItems[obj] as ListViewItem;
                    if (lv != null)
                    {
                        ObjectValueItems.Remove(obj);
                        lv.Remove();
                    }
                    TreeNode node = ObjectTreeItems[obj] as TreeNode;
                    if (node != null)
                    {
                        //If this is the last node to remove.
                        if (node.Parent.Nodes.Count == 1 && !(node.Parent.Tag is GXDLMSDevice))
                        {
                            object type = (obj.Parent.Tag.GetHashCode() << 16) + obj.ObjectType;
                            TreeNode node2 = ObjectTreeItems[type] as TreeNode;
                            if (node2 != null)
                            {
                                node2.Remove();
                            }
                            ObjectTreeItems.Remove(type);
                        }
                        else
                        {
                            node.Remove();
                        }
                        ObjectTreeItems.Remove(obj);
                    }
                    (obj.Parent.Tag as GXDLMSMeter).Objects.Remove(obj);
                    ListViewItem item = ObjectListItems[obj] as ListViewItem;
                    if (item != null)
                    {
                        if (SelectedListItems.Values.Contains(obj))
                        {
                            SelectedListItems.RemoveAt(SelectedListItems.IndexOfValue(obj));
                        }
                        item.Remove();
                        ObjectListItems.Remove(obj);
                    }
                }
                else if (target is GXDLMSObjectCollection)
                {
                    GXDLMSObjectCollection items = target as GXDLMSObjectCollection;
                    if (items.Count != 0)
                    {
                        foreach (GXDLMSObject it in items)
                        {
                            RemoveObject(it);
                        }
                    }
                }
                else if (target is GXDLMSDevice)
                {
                    GXDLMSDevice dev = target as GXDLMSDevice;
                    while (dev.Objects.Count != 0)
                    {
                        RemoveObject(dev.Objects[0]);
                    }
                }
            }
        }

        /// <summary>
        /// Delete selected object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteMnu_Click(object sender, EventArgs e)
        {
            try
            {
                if (ObjectTree.Focused)
                {
                    TreeNode node = ObjectTree.SelectedNode;
                    if (node != null)
                    {
                        object obj = node.Tag;
                        if (obj is GXDLMSDevice)
                        {
                            if (MessageBox.Show(this, GXDLMSDirector.Properties.Resources.RemoveDeviceConfirmation, GXDLMSDirector.Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                            {
                                return;
                            }
                            RemoveDevice(obj as GXDLMSDevice);
                        }
                        else if (obj is GXDLMSObject)
                        {
                            if (MessageBox.Show(this, GXDLMSDirector.Properties.Resources.RemoveObjectConfirmation, GXDLMSDirector.Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                            {
                                return;
                            }
                            RemoveObject(obj);
                        }
                        else if (obj is GXDLMSObjectCollection)
                        {
                            if (MessageBox.Show(this, GXDLMSDirector.Properties.Resources.RemoveObjectConfirmation, GXDLMSDirector.Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                            {
                                return;
                            }
                            RemoveObject(obj);
                        }
                        //Find selected device from the device list.
                        else if (obj is GXDLMSDeviceCollection)
                        {
                            if (MessageBox.Show(this, GXDLMSDirector.Properties.Resources.RemoveDeviceConfirmation, GXDLMSDirector.Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                            {
                                return;
                            }
                            foreach (ListViewItem it in DeviceList.SelectedItems)
                            {
                                RemoveDevice(it.Tag as GXDLMSDevice);
                            }
                        }
                    }
                }
                else if (ObjectList.Focused && ObjectList.SelectedItems.Count != 0)
                {
                    if (MessageBox.Show(this, GXDLMSDirector.Properties.Resources.RemoveObjectConfirmation, GXDLMSDirector.Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                    {
                        return;
                    }

                    GXDLMSObjectCollection items = new GXDLMSObjectCollection();
                    foreach (ListViewItem it in ObjectList.SelectedItems)
                    {
                        items.Add(it.Tag as GXDLMSObject);
                    }
                    foreach (GXDLMSObject it in items)
                    {
                        RemoveObject(it);
                    }
                }
                SetDirty(true);
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ConnectBtn.Checked)
                {
                    ConnectMnu_Click(null, null);
                }
                else
                {
                    DisconnectMnu_Click(null, null);
                }
               
            }
            catch (Exception Ex)
            {
                textBox1.Text = "on capture";
                GXDLMS.Common.Error.ShowError(this, Ex);
              
            }
            textBox1.Text = " " + visuel;
        }

        internal static void UpdateAttributes(GXDLMSObject obj, GXObisCode it)
        {
            obj.Version = it.Version;
            obj.LogicalName = it.LogicalName;
            foreach (GXDLMSAttributeSettings a in it.Attributes)
            {
                if (a.UIType != DataType.None)
                {
                    obj.SetUIDataType(a.Index, a.UIType);
                }
                if (a.Type != DataType.None)
                {
                    obj.SetDataType(a.Index, a.Type);
                }
                obj.SetValues(a.Index, a.Values);
                obj.SetAccess(a.Index, a.Access);
                obj.SetXml(a.Index, a.Xml);
                obj.SetUIValueType(a.Index, a.UIValueType);
            }
        }

        TreeNode AddDevice(GXDLMSMeter dev, bool refresh, bool first)
        {
            if (!refresh)
            {
                GXDLMSDevice d = dev as GXDLMSDevice;
                if (d != null)
                {
                    d.Media.OnReceived += new ReceivedEventHandler(Events_OnReceived);
                    d.OnProgress += new ProgressEventHandler(this.OnProgress);
                    d.OnStatusChanged += new StatusEventHandler(this.OnStatusChanged);
                }
                GXManufacturer m = Manufacturers.FindByIdentification(dev.Manufacturer);
                if (first && m.ObisCodes != null)
                {
                    GXDLMSConverter c = new GXDLMSConverter();
                    foreach (GXObisCode it in m.ObisCodes)
                    {
                        if (it.Append)
                        {
                            GXDLMSObject obj = GXDLMSClient.CreateObject(it.ObjectType);
                            if (string.IsNullOrEmpty(it.Description))
                            {
                                obj.Description = c.GetDescription(it.LogicalName, it.ObjectType)[0];
                            }
                            else
                            {
                                obj.Description = it.Description;
                            }
                            UpdateAttributes(obj, it);
                            dev.Objects.Add(obj);
                        }
                    }
                }
            }
            //Add device to device tree.
            TreeNode node = this.ObjectTree.Nodes[0].Nodes.Add(dev.Name);
            node.SelectedImageIndex = node.ImageIndex = 2;
            node.Tag = dev;
            ObjectTreeItems[dev] = node;
            //Add device to device list.
            if (!refresh)
            {
                ListViewItem item = DeviceList.Items.Add(dev.Name);
                item.Tag = dev;
                DeviceListViewItems[dev] = item;
            }
            return node;
        }

        /// <summary>
        /// Load settings from the file.
        /// </summary>
        /// <param name="path"></param>
         void LoadFile(string path)
        {
            int version = 1;
            if (activeDC != null)
            {
                Devices.Clear();
                Devices.AddRange(activeDC.GetDevices(null));
            }
            else
            {
                DialogResult ret = DialogResult.OK;
                //Ask user to close active connections.
                foreach (GXDLMSDevice it in Devices)
                {
                    if (it.Comm.media.IsOpen)
                    {
                        MessageBox.Show(this, Properties.Resources.OpenConnectionsTxt, Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                        if (ret == DialogResult.Cancel)
                        {
                            return;
                        }
                        break;
                    }
                }
                if (ret == DialogResult.Yes)
                {
                    foreach (GXDLMSDevice it in Devices)
                    {
                        try
                        {
                            it.Disconnect();
                        }
                        catch (Exception)
                        {
                            //Skip all errors.
                        }
                    }
                }

                //Save changes?
                if (this.Dirty)
                {
                    ret = MessageBox.Show(this, Properties.Resources.SaveChangesTxt, Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ret == DialogResult.Cancel)
                    {
                        return;
                      
                    }
                    if (ret == DialogResult.Yes)
                    {
                        if (!Save())
                        {
                            return;
                        }
                    }
                    //Don't show save changes again.
                    this.Dirty = false;
                }
                //Clear log every time when new device list is loaded.
                GXLogWriter.ClearLog();
                NewMnu_Click(null, null);
                using (XmlReader reader = XmlReader.Create(path))
                {
                    List<Type> types = new List<Type>(Gurux.DLMS.GXDLMSClient.GetObjectTypes());
                    types.Add(typeof(GXDLMSAttributeSettings));
                    types.Add(typeof(GXDLMSAttribute));
                    //Version is added to namespace.
                    XmlSerializer x = new XmlSerializer(typeof(GXDLMSDeviceCollection), null, types.ToArray(), null, "Gurux1");
                    if (!x.CanDeserialize(reader))
                    {
                        version = 0;
                        x = new XmlSerializer(typeof(GXDLMSDeviceCollection), types.ToArray());
                    }
                    //-----------------------------Debut de la modification---------------------


                    GXDLMSMeterCollection deva = new GXDLMSMeterCollection();
                    foreach (GXDLMSMeter g in Devices)
                    {
                        deva.Add(g);
                    }

                    Devices.Clear();
                    ObjectValueView.Items.Clear();

                    Devices.AddRange((GXDLMSDeviceCollection)x.Deserialize(reader));


                    // textBox1.Text = deva.Count + " ciciciic    ";
                    if (deva.Count != 0)
                    {
                        foreach (GXDLMSMeter g in Devices)
                        {
                            int i = 0;
                            bool test = false;
                            foreach (GXDLMSMeter h in deva)
                            {
                                if (g.Name.Equals(h.Name))
                                {
                                    //textBox1.Text = "test d'entree";
                                    i = deva.IndexOf(h);

                                    test = true;

                                }

                            }
                            if (test)
                            {
                                // textBox1.Text = " ->>>>" + i + "---->";
                                deva.RemoveAt(i);

                            }



                        }

                        Devices.AddRange(deva);
                    }

                    reader.Close();
                    //textBox1.Text += Devices.Count + " sa se termine la ";

                    //--------------------------Fin de la modification ------------------------
                    TreeNode node = ObjectTree.Nodes[0];
                    node.Tag = Devices;
                }
            }
            //Add devices to the device tree and update parser.
            foreach (GXDLMSMeter dev in Devices)
            {
                GXDLMSDevice d = dev as GXDLMSDevice;
                d.Comm.client.Standard = dev.Standard;
                //Conformance is new funtionality. Set default value if not set.
                if (dev.UseLogicalNameReferencing && dev.Conformance == (int)GXDLMSClient.GetInitialConformance(false))
                {
                    dev.Conformance = (int)GXDLMSClient.GetInitialConformance(dev.UseLogicalNameReferencing);
                }
                if (d != null)
                {
                    d.Comm.parentForm = this;
                    d.Manufacturers = this.Manufacturers;
                }
                GXManufacturer m = Manufacturers.FindByIdentification(dev.Manufacturer);
                if (m == null)
                {
                    throw new Exception("Load failed. Invalid manufacturer: " + dev.Manufacturer);
                }
                if (version == 0)
                {
                    dev.UseLogicalNameReferencing = m.UseLogicalNameReferencing;
                }
                dev.Objects.Tag = dev;
                if (d != null)
                {
                    d.ObisCodes = m.ObisCodes;
                }
                AddDevice(dev, false, false);
                RefreshDevice(dev, false);
                //Update association views.
                GXDLMSObject it = dev.Objects.FindByLN(ObjectType.AssociationLogicalName, "0.0.40.0.0.255");
                if (it is GXDLMSAssociationLogicalName)
                {
                    (it as GXDLMSAssociationLogicalName).ObjectList.AddRange(dev.Objects);
                }
            }
            GroupItems(GroupsMnu.Checked, null);
            this.path = path;
            SetDirty(false);
            mruManager.Insert(0, path);
        }

        /// <summary>
        /// Open media settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenMnu_Click(object sender, EventArgs e)
        {
            string file = null;
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Multiselect = true;
                if (string.IsNullOrEmpty(path))
                {
                    dlg.InitialDirectory = Directory.GetCurrentDirectory();
                }
                else
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(path);
                    dlg.InitialDirectory = fi.DirectoryName;
                    dlg.FileName = fi.Name;
                }
                dlg.Filter = Properties.Resources.FilterTxt;
                dlg.DefaultExt = ".gdr";
                dlg.ValidateNames = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    file = dlg.FileName;
                    if (File.Exists(file))
                    {
                        LoadFile(file);
                    }
                }
            }
            catch (Exception Ex)
            {
                if (file != null)
                {
                    mruManager.Remove(file);
                }
                if (Ex.InnerException != null)
                {
                    Ex = Ex.InnerException;
                }
                GXLogWriter.WriteLog(Ex.ToString());
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        void UpdateFromObisCode(GXDLMSObject obj, GXObisCode item)
        {
            if (!String.IsNullOrEmpty(item.Description))
            {
                obj.Description = item.Description;
            }
            if (item.Attributes.Count != 0)
            {
                foreach (var it in item.Attributes)
                {
                    GXDLMSAttributeSettings a = obj.Attributes.Find(it.Index);
                    if (a != null)
                    {
                        obj.Attributes.Remove(a);
                    }
                    obj.Attributes.Add(it);
                }
            }
            foreach (GXDLMSAttributeSettings it in item.Attributes)
            {
                if (obj.Attributes.Find(it.Index) == null)
                {
                    obj.Attributes.Add(it);
                }
            }
        }

        delegate void ClearTraceEventHandler();

        void ClearTrace()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ClearTraceEventHandler(this.ClearTrace));
            }
            else
            {
                TraceView.ResetText();
            }
        }

        void RefreshDevice(GXDLMSMeter dev, bool bRefresh)
        {
            GXDLMSDevice d = dev as GXDLMSDevice;
            try
            {
                ClearTrace();
                if (bRefresh && d != null)
                {
                    d.KeepAliveStop();
                }
                TreeNode deviceNode = (TreeNode)ObjectTreeItems[dev];
                if (bRefresh)
                {
                    OnProgress(dev, "Refresh device", 0, 1);
                    UpdateTransaction(true);
                    while (dev.Objects.Count != 0)
                    {
                        RemoveObject(dev.Objects[0]);
                    }
                    GXManufacturer m = Manufacturers.FindByIdentification(dev.Manufacturer);
                    //Walk through object tree.
                    if (d != null)
                    {
                        d.UpdateObjects();
                    }
                    else
                    {
                        GXDLMSAssociationLogicalName ln = new GXDLMSAssociationLogicalName();
                        List<KeyValuePair<GXDLMSObject, byte>> list = new List<KeyValuePair<GXDLMSObject, byte>>();
                        list.Add(new KeyValuePair<GXDLMSObject, byte>(ln, 2));
                        activeDC.ReadObjects(new GXDLMSMeter[] { dev }, list);
                        dev.Objects.Clear();
                        //Add objects one at the time because we need to update the parent list.
                        while (ln.ObjectList.Count != 0)
                        {
                            GXDLMSObject obj = ln.ObjectList[0];
                            ln.ObjectList.RemoveAt(0);
                            dev.Objects.Add(obj);
                        }
                    }
                    //Read registers units and scalers.
                    int cnt = dev.Objects.Count;
                    for (int pos = 0; pos != cnt; ++pos)
                    {
                        GXDLMSObject it = dev.Objects[pos];
                        GXObisCode obj = m.ObisCodes.FindByLN(it.ObjectType, it.LogicalName, null);
                        if (obj != null)
                        {
                            UpdateFromObisCode(it, obj);
                        }
                        else
                        {
                            continue;
                        }

                        //Update association views.
                        if (it is GXDLMSAssociationLogicalName && it.LogicalName == "0.0.40.0.4.255")
                        {
                            (it as GXDLMSAssociationLogicalName).ObjectList.AddRange(dev.Objects);
                        }
                    }
                    //Read inactivity timeout.
                    if (d != null)
                    {
                        dev.InactivityTimeout = 120 - 10;
                        foreach (GXDLMSHdlcSetup it in dev.Objects.GetObjects(ObjectType.IecHdlcSetup))
                        {
                            d.Comm.ReadValue(it, 8);
                            if (dev.InactivityTimeout > it.InactivityTimeout && it.InactivityTimeout > 10)
                            {
                                dev.InactivityTimeout = it.InactivityTimeout - 10;
                            }
                        }
                        this.OnProgress(dev, "Reading scalers and units.", cnt, cnt);
                    }
                    GroupItems(GroupsMnu.Checked, dev);
                }
            }
            catch (ThreadAbortException)
            {
                //User has cancel action. Do nothing.
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
            finally
            {
                if (d != null && bRefresh)
                {
                    d.KeepAliveStart();
                    UpdateTransaction(false);
                }
            }
        }

        delegate ListViewItem AddNodeEventHandler(GXDLMSObject it, TreeNode deviceNode, ListViewGroup group);

        ListViewItem AddNode(GXDLMSObject it, TreeNode deviceNode, ListViewGroup group)
        {
            if (this.InvokeRequired)
            {
                return this.Invoke(new AddNodeEventHandler(AddNode), new object[] { it, deviceNode, group }) as ListViewItem;
            }
            TreeNode node;
            GXDLMSObjectCollection objects = null;
            if (group != null)
            {
                node = ObjectTreeItems[(it.Parent.Tag.GetHashCode() << 16) + it.ObjectType] as TreeNode;
                if (node == null)
                {
                    node = deviceNode.Nodes.Add(it.ObjectType.ToString());
                    node.SelectedImageIndex = node.ImageIndex = 11;
                    ObjectTreeItems[(it.Parent.Tag.GetHashCode() << 16) + it.ObjectType] = node;
                    objects = new GXDLMSObjectCollection();
                    objects.Tag = deviceNode.Tag;
                    node.Tag = objects;
                }
                else
                {
                    objects = node.Tag as GXDLMSObjectCollection;
                }
                objects.Add(it);
                node = node.Nodes.Add(it.LogicalName + " " + it.Description);
            }
            else
            {
                node = deviceNode.Nodes.Add(it.LogicalName + " " + it.Description);
            }
            node.Tag = it;
            ObjectTreeItems[it] = node;
            ListViewItem item = new ListViewItem(it.LogicalName + " " + it.Description);
            item.Tag = it;
            item.Group = group;
            ObjectListItems.Add(it, item);
            if (it is GXDLMSProfileGeneric)
            {
                node.SelectedImageIndex = node.ImageIndex = 8;
            }
            else
            {
                node.SelectedImageIndex = node.ImageIndex = 7;
                routineStatu(node);
            }
            return item;
        }

        delegate void GetDevicesEventHandler(string path);

        void OnGetDevices(string path)
        {
            LoadFile(null);
        }


        void Refresh(object sender, GXAsyncWork work, object[] parameters)
        {
            try
            {
                if (parameters[0] is GXDLMSMeterCollection)
                {
                    if (activeDC != null)
                    {
                        this.BeginInvoke(new GetDevicesEventHandler(OnGetDevices), "");
                    }
                    else
                    {
                        foreach (GXDLMSMeter dev in (GXDLMSMeterCollection)parameters[0])
                        {
                            RefreshDevice(dev, true);
                        }
                    }
                }
                else if (parameters[0] is GXDLMSMeter)
                {
                    GXDLMSMeter dev = (GXDLMSMeter)parameters[0];
                    RefreshDevice(dev, true);
                }
                else if (parameters[0] is GXDLMSObjectCollection)
                {
                    bool oneProfileGeneric = false;
                    bool allProfileGeneric = true;
                    //If only profile generics are selected update only them not whole device.
                    GXDLMSObjectCollection items = parameters[0] as GXDLMSObjectCollection;
                    foreach (GXDLMSObject it in items)
                    {
                        if (!(it is GXDLMSProfileGeneric))
                        {
                            allProfileGeneric = false;
                            if (oneProfileGeneric)
                            {
                                break;
                            }
                        }
                        else
                        {
                            oneProfileGeneric = true;
                        }
                    }
                    if (oneProfileGeneric && allProfileGeneric)
                    {
                        foreach (GXDLMSProfileGeneric pg in items)
                        {
                            GXDLMSDevice dev = pg.Parent.Tag as GXDLMSDevice;
                            dev.UpdateColumns(pg, dev.Manufacturers.FindByIdentification(dev.Manufacturer));
                            if (items.Count != 1)
                            {
                                ListViewItem it = ObjectListItems[pg] as ListViewItem;
                                it.Selected = false;
                            }
                        }
                    }
                    else if (items.Count != 0)//If other than profile generics are selected read meter.
                    {
                        GXDLMSMeter dev = items[0].Parent.Tag as GXDLMSMeter;
                        Refresh(sender, work, new object[] { dev });
                    }
                }
                else if (parameters[0] is GXDLMSProfileGeneric)
                {
                    ClearTrace();
                    GXDLMSProfileGeneric pg = (GXDLMSProfileGeneric)parameters[0];
                    GXDLMSDevice dev = pg.Parent.Tag as GXDLMSDevice;
                    dev.UpdateColumns(pg, dev.Manufacturers.FindByIdentification(dev.Manufacturer));
                    ((GXDLMSProfileGenericView)SelectedView).Target = parameters[0] as GXDLMSProfileGeneric;
                    if (InvokeRequired)
                    {
                        this.Invoke(new ValueChangedEventHandler(OnValueChanged), new object[] { SelectedView, 3, null, false, (dev.Status & DeviceState.Connected) != 0 });
                    }
                    else
                    {
                        SelectedView.OnValueChanged(3, null, false, (dev.Status & DeviceState.Connected) != 0);
                    }
                }
                else
                {
                    GXDLMSObject obj = parameters[0] as GXDLMSObject;
                    GXDLMSMeter dev = obj.Parent.Tag as GXDLMSMeter;
                    if (!this.InvokeRequired)
                    {
                        ObjectTree.SelectedNode = ObjectTreeItems[dev] as TreeNode;
                    }
                    RefreshDevice(dev, true);
                }
                SetDirty(true);
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(sender as Form, Ex);
            }
        }

        /// <summary>
        /// Update object tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (activeDC != null)
            {
                Refresh(null, null, new object[] { ObjectTree.SelectedNode.Tag });
            }
            else
            {
                if (tabControl1.SelectedIndex == 0)
                {
                    TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Refresh, OnError, null, new object[] { ObjectTree.SelectedNode.Tag });
                }
                else
                {
                    GXDLMSObjectCollection items = new GXDLMSObjectCollection();
                    foreach (ListViewItem it in ObjectList.SelectedItems)
                    {
                        items.Add(it.Tag as GXDLMSObject);
                    }
                    TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Refresh, OnError, null, new object[] { items });
                }
                TransactionWork.Start();
            }
        }

        private void AddDevice(GXDLMSDevice dev)
        {
            DevicePropertiesForm dlg = new DevicePropertiesForm(this.Manufacturers, dev);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                (dlg.Device as GXDLMSDevice).Manufacturers = this.Manufacturers;
                (dlg.Device as GXDLMSDevice).Comm.parentForm = this;
                AddDevice((GXDLMSDevice)dlg.Device, false, dev == null);
                Devices.Add((GXDLMSDevice)dlg.Device);
                GroupItems(GroupsMnu.Checked, null);
                SetDirty(true);
            }
        }

        private void AddDeviceMnu_Click(object sender, EventArgs e)
        {
            try
            {
                AddDevice(null);
                //Add OBIS codes.
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void ContentsMnu_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://www.gurux.fi/index.php?q=GXDLMSDirectorHelp");
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Edit manufacturers settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManufacturersMnu_Click(object sender, EventArgs e)
        {
            string selectedManufacturer = null;
            GXDLMSMeter dev = GetSelectedDevice();
            if (dev != null)
            {
                selectedManufacturer = dev.Manufacturer;
            }
            ManufacturersForm dlg = new ManufacturersForm(this.Manufacturers, selectedManufacturer);
            dlg.ShowDialog(this);
        }

        private void OBISCodesMnu_Click(object sender, EventArgs e)
        {
            //   textBox1.Text = "entre ici en premier";
            string ln = null;
            string selectedManufacturer = null;
            TreeNode node = this.ObjectTree.SelectedNode;
            ObjectType Interface = ObjectType.None;
            GXDLMSDevice dev = null;
            if (node != null)
            {
                if (node.Tag is GXDLMSDevice)
                {
                    dev = (GXDLMSDevice)node.Tag;
                    selectedManufacturer = dev.Manufacturer;
                }
                else if (this.ObjectTree.SelectedNode.Tag is GXDLMSObject)
                {
                    GXDLMSObject obj = this.ObjectTree.SelectedNode.Tag as GXDLMSObject;
                    dev = obj.Parent.Tag as GXDLMSDevice;
                    selectedManufacturer = dev.Manufacturer;
                    ln = obj.LogicalName;
                    Interface = obj.ObjectType;
                }
            }
            OBISCodesForm dlg = new OBISCodesForm(this.Manufacturers, selectedManufacturer, Interface, ln);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (this.ObjectTree.SelectedNode.Tag is GXDLMSObject)
                {
                    GXDLMSObject obj = this.ObjectTree.SelectedNode.Tag as GXDLMSObject;
                    GXObisCode code = this.Manufacturers.FindByIdentification(selectedManufacturer).ObisCodes.FindByLN(obj.ObjectType, obj.LogicalName, null);
                    if (code != null)
                    {
                        obj.Description = code.Description;
                        foreach (GXDLMSAttributeSettings it in code.Attributes)
                        {
                            GXDLMSAttributeSettings att = obj.Attributes.Find(it.Index);
                            if (att == null)
                            {
                                obj.Attributes.Add(it);
                            }
                            else if (att.GetType() == typeof(GXDLMSAttribute))
                            {
                                obj.Attributes.Remove(it);
                                GXDLMSAttributeSettings tmp = new GXDLMSAttributeSettings();
                                it.CopyTo(tmp);
                                obj.Attributes.Add(tmp);
                            }
                            else
                            {
                                it.CopyTo(att);
                            }
                        }
                    }
                }
            }
        }

        private void LogMnu_Click(object sender, EventArgs e)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "notepad";
                p.StartInfo.FileName = GXLogWriter.LogPath;
                p.Start();
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void ClearLogMnu_Click(object sender, EventArgs e)
        {
            try
            {
                GXLogWriter.ClearLog();
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void ObjectPanelFrame_Resize(object sender, EventArgs e)
        {
        }




        private void ObjectTree_MouseUp(object sender, MouseEventArgs e)
        {
            //   textBox1.Text += "on entre ici en premier";
            if (e.Button == MouseButtons.Right)
            {
                Point pt = new Point(e.X, e.Y);
                ObjectTree.PointToClient(pt);

                TreeNode Node = ObjectTree.GetNodeAt(pt);
                if (Node != null)
                {
                    if (Node.Bounds.Contains(pt))
                    {
                        ObjectTree.SelectedNode = Node;
                        ConfigureContextMenu();

                        // textBox1.Text = " " + contextMenuStrip1.Visible;

                        // this.addRoutine.Text = " ne pas ajouter a la routine";
                        try
                        {
                            if (ObjectTree.SelectedNode.Parent.Text.Equals("Devices"))
                            {
                                addRoutine.Enabled = false;
                                this.ExportMenu.Visible = true;
                                this.ImportMenu.Visible = true;
                                this.DeleteCMnu.Visible = true;
                                this.PropertiesCMnu.Visible = true;
                                this.toolStripMenuItem32.Visible = true;
                                this.toolStripMenuItem5.Visible = true;
                            }
                            else if ((ObjectTree.SelectedNode.Text.Equals("Devices")))
                            {
                                addRoutine.Enabled = false;
                                 this.ExportMenu.Visible = false;
                                this.ImportMenu.Visible = false;
                                this.DeleteCMnu.Visible = false;
                                this.PropertiesCMnu.Visible = false;
                            } else
                            {
                                addRoutine.Enabled = true;
                                this.ExportMenu.Visible = false;
                                this.ImportMenu.Visible = false;
                                this.DeleteCMnu.Visible =false;
                                this.PropertiesCMnu.Visible = false;
                            }
                        }
                        catch
                        {
                            addRoutine.Enabled = false;
                            this.ExportMenu.Visible = false;
                            this.ImportMenu.Visible = false;
                            this.DeleteCMnu.Visible = false;
                            this.PropertiesCMnu.Visible = false;
                        }

                        if ((ObjectTree.SelectedNode.Text.Equals("Devices")))
                        {
                            this.ConnectCMnu.Visible = false;
                            this.DisconnectCMnu.Visible = false;
                            this.ReadCMnu.Visible = false;
                            this.ReadObjectMnu.Visible = false;
                            this.addRoutine.Visible = false;
                            this.AddDeviceCMnu.Visible = true;
                            this.toolStripMenuItem32.Visible = false;
                            this.ImportMenu.Visible = false;
                            this.ExportMenu.Visible = false;
                            this.Line1CMnu.Visible = false;
                            this.DeleteCMnu.Visible = false;
                          //  this.AddDeviceMnu.Visible = false;
                            this.toolStripMenuItem5.Visible = false;
                            this.PropertiesCMnu.Visible = false;

                        }
                        else
                        {
                            this.ConnectCMnu.Visible = true;
                            this.DisconnectCMnu.Visible = true;
                            this.ReadCMnu.Visible = true;
                            this.ReadObjectMnu.Visible = true;
                            this.addRoutine.Visible = true;
                            this.AddDeviceCMnu.Visible = false;
                            this.toolStripMenuItem32.Visible = false;
                           // this.ImportMenu.Visible = true;
                         //   this.ExportMenu.Visible = true;
                            this.Line1CMnu.Visible =false;
                           // this.DeleteCMnu.Visible = true;
                          //  this.AddDeviceMnu.Visible = false;
                            this.toolStripMenuItem5.Visible = false;
                          //  this.PropertiesCMnu.Visible = true;
                        }
                        contextMenuStrip1.Show(ObjectTree, pt);
                        //textBox1.Text += " " + contextMenuStrip1.Visible;



                    }
                }
            }

        }

        private void testVisible()
        {

        }

        private void ConfigureContextMenu()
        {
            ConnectCMnu.Visible = false;
            DisconnectCMnu.Visible = false;
            ReadCMnu.Visible = false;
            AddDeviceCMnu.Visible = false;
            DeleteCMnu.Visible = false;
            Line1CMnu.Visible = false;

            if (this.ObjectTree.SelectedNode != null)
            {
                if (this.ObjectTree.SelectedNode.Tag is GXDLMSDeviceCollection)
                {
                    AddDeviceCMnu.Visible = true;
                    return;
                }
                ConnectCMnu.Visible = true;
                DisconnectCMnu.Visible = true;
                ReadCMnu.Visible = true;
                DeleteCMnu.Visible = true;
                Line1CMnu.Visible = true;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                //Get the normal command lines arguments in case the EXE is called directly
                List<string> list = new List<string>();
                // this is how arguments are passed from windows explorer to clickonce installed apps when files are associated in explorer
                if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments?.ActivationData != null)
                {
                    foreach (string it in AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData)
                    {
                        list.AddRange(it.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    list.AddRange(Environment.GetCommandLineArgs());
                    list.RemoveAt(0);
                }

                events = new GXNet(NetworkType.Tcp, 4059);
                events.ConfigurableSettings = (AvailableMediaSettings.Port | AvailableMediaSettings.Protocol);
                events.Settings = Properties.Settings.Default.EventsSettings;
                events.OnMediaStateChange += Events_OnMediaStateChange;
                events.OnReceived += Events_OnReceived;
                events.OnClientConnected += Events_OnClientConnected;
                events.OnClientDisconnected += Events_OnClientDisconnected;
                Application.EnableVisualStyles();
                ObjectValueView.Columns.Clear();
                ObjectValueView.Columns.Add("Name");
                ObjectValueView.Columns.Add("Object Type");
                ObjectValueView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                LoadXmlPositioning();

                Views = GXDlmsUi.GetViews(ObjectPanelFrame, OnHandleAction);

                if (GXManufacturerCollection.IsFirstRun())
                {
                    if (MessageBox.Show(this, GXDLMSDirector.Properties.Resources.InstallManufacturersOnlineTxt, GXDLMSDirector.Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
                    {
                        GXManufacturerCollection.UpdateManufactureSettings();
                    }
                }
                Manufacturers = new GXManufacturerCollection();
                GXManufacturerCollection.ReadManufacturerSettings(Manufacturers);
                ThreadPool.QueueUserWorkItem(new WaitCallback(CheckUpdates), this);

                //Load conformace tests.
                string path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
                string testResults = Path.Combine(path2, "TestResults");
                if (Directory.Exists(testResults))
                {
                    var di = new DirectoryInfo(testResults);
                    var list2 = di.EnumerateDirectories()
                                        .OrderBy(d => d.CreationTime)
                                        .Select(d => d.Name)
                                        .ToList();
                    foreach (string it in list2)
                    {
                        GXConformanceTest t = new GXConformanceTest();
                        t.OnReady = OnConformanceReady;
                        t.OnError = OnConformanceError;
                        t.OnTrace = OnConformanceTrace;
                        t.OnProgress = OnProgress;
                        t.Results = Path.Combine(testResults, it);
                        ListViewItem li = ConformanceHistoryTests.Items.Insert(0, Path.GetFileName(it));
                        li.SubItems.Add("");
                        li.Tag = t;
                    }
                }
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type type in a.GetTypes())
                        {
                            if (!type.IsAbstract && type.IsClass && typeof(IGXDataConcentrator).IsAssignableFrom(type))
                            {
                                IGXDataConcentrator dc = (IGXDataConcentrator)a.CreateInstance(type.ToString());
                                System.Windows.Forms.ToolStripMenuItem it = new System.Windows.Forms.ToolStripMenuItem();
                                it.Name = it.Text = dc.Name;
                                it.Click += It_Click;
                                it.Tag = dc;
                                DataConcentratorsMnu.DropDownItems.Add(it);
                                if (Properties.Settings.Default.ActiveDC == dc.Name)
                                {
                                    It_Click(it, null);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //It's OK if this fails.
                    }
                }
                DataConcentratorsMnu.Visible = DataConcentratorsMnu.DropDownItems.Count != 0;
                try
                {
                    if (list.Count != 0)
                    {
                        if (list[0] == "-h" || list[0] == "/h")
                        {
                            ShowHelp();
                            Close();
                            return;
                        }
                        this.path = list[0];
                        LoadFile(this.path);
                        list.RemoveAt(0);
                        bool showHelp;
                        CloseApp closeApp;
                        string output, settingsFile;
                        List<string> files = new List<string>();
                        bool ctt;
                        GetParameters(list.ToArray(), files, out output, out settingsFile, out showHelp, out closeApp, out ctt);
                        if (showHelp)
                        {
                            Close();
                            return;
                        }
                        foreach (string it in files)
                        {
                            if (!File.Exists(it))
                            {
                                throw new Exception("File don't exists. " + it);
                            }
                        }
                        GXConformanceSettings settings;
                        XmlSerializer x = new XmlSerializer(typeof(GXConformanceSettings));
                        if (ctt)
                        {
                            if (string.IsNullOrEmpty(settingsFile))
                            {
                                if (Properties.Settings.Default.ConformanceSettings == "")
                                {
                                    settings = new GXConformanceSettings();
                                }
                                else
                                {
                                    try
                                    {
                                        using (StringReader reader = new StringReader(Properties.Settings.Default.ConformanceSettings))
                                        {
                                            settings = (GXConformanceSettings)x.Deserialize(reader);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine(ex.Message);
                                        settings = new GXConformanceSettings();
                                    }
                                }
                                settings.WarningBeforeStart = false;
                                settings.CloseApplication = closeApp;
                            }
                            else
                            {
                                using (StringReader reader = new StringReader(settingsFile))
                                {
                                    settings = (GXConformanceSettings)x.Deserialize(reader);
                                    settings.WarningBeforeStart = false;
                                    settings.CloseApplication = closeApp;
                                }
                            }
                            if (RunConformanceTest(settings, Devices, false, output))
                            {
                                try
                                {
                                    using (StringWriter writer = new StringWriter())
                                    {
                                        x.Serialize(writer, settings);
                                        Properties.Settings.Default.ConformanceSettings = writer.ToString();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine(ex.Message);
                                    Properties.Settings.Default.ConformanceSettings = "";
                                }
                            }
                        }
                        else
                        {
                            foreach (string it in files)
                            {
                                if (string.IsNullOrEmpty(settingsFile))
                                {
                                    if (Properties.Settings.Default.ConformanceSettings == "")
                                    {
                                        settings = new GXConformanceSettings();
                                    }
                                    else
                                    {
                                        try
                                        {
                                            using (StringReader reader = new StringReader(Properties.Settings.Default.ConformanceSettings))
                                            {
                                                settings = (GXConformanceSettings)x.Deserialize(reader);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine(ex.Message);
                                            settings = new GXConformanceSettings();
                                        }
                                    }
                                    settings.WarningBeforeStart = false;
                                    settings.CloseApplication = closeApp;
                                    settings.ExternalTests = it;
                                    settings.ExcludedApplicationTests.Set(true);
                                    settings.ExcludedHdlcTests.Set(true);
                                    settings.ExcludeBasicTests = settings.ExcludeMeterInfo = true;
                                }
                                else
                                {
                                    using (StringReader reader = new StringReader(settingsFile))
                                    {
                                        settings = (GXConformanceSettings)x.Deserialize(reader);
                                        settings.CloseApplication = closeApp;
                                        settings.WarningBeforeStart = false;
                                    }
                                }
                                if (RunConformanceTest(settings, Devices, false, output))
                                {
                                    try
                                    {
                                        using (StringWriter writer = new StringWriter())
                                        {
                                            x.Serialize(writer, settings);
                                            Properties.Settings.Default.ConformanceSettings = writer.ToString();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine(ex.Message);
                                        Properties.Settings.Default.ConformanceSettings = "";
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    GXDLMS.Common.Error.ShowError(this, Ex);
                    Close();
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        static int GetParameters(string[] args, List<string> files, out string output, out string settings, out bool showHelp, out CloseApp closeApp, out bool ctt)
        {
            ctt = false;
            closeApp = CloseApp.Never;
            showHelp = false;
            output = null;
            settings = null;
            try
            {
                List<GXCmdParameter> parameters = GXCommon.GetParameters(args, "x:o:s:hc:t");
                foreach (GXCmdParameter it in parameters)
                {
                    switch (it.Tag)
                    {
                        case 't':
                            ctt = true;
                            break;
                        case 'c':
                            try
                            {
                                closeApp = (CloseApp)Enum.Parse(typeof(CloseApp), it.Value);
                            }
                            catch (Exception)
                            {
                                throw new Exception("Invalid close value. (Never, Always, Success");
                            }
                            break;
                        case 'x':
                            files.Add(it.Value);
                            break;
                        case 'o':
                            output = it.Value;
                            if (!Directory.Exists(output))
                            {
                                throw new Exception("Output directory don't exists. " + output);
                            }
                            break;
                        case 's':
                            settings = it.Value;
                            if (!File.Exists(settings))
                            {
                                throw new Exception("settings file don't exists. " + settings);
                            }
                            break;
                        case '?':
                            switch (it.Tag)
                            {
                                case 'c':
                                    closeApp = CloseApp.Always;
                                    break;
                                case 'x':
                                    throw new ArgumentException("Missing mandatory test file.");
                                case 'o':
                                    throw new ArgumentException("Missing mandatory output folder.");
                                default:
                                    ShowHelp();
                                    return 1;
                            }
                            break;
                        case 'h':
                        default:
                            showHelp = true;
                            ShowHelp();
                            return 1;
                    }
                }
            }
            catch (Exception)
            {
                showHelp = true;
                ShowHelp();
                return 1;
            }
            return 0;
        }

        static void ShowHelp()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Command line parameter for GXDLMSDirector to execute Conformance Tests.");
            sb.AppendLine("%userprofile%\\Desktop\\GXDLMSDirector.appref-ms \"C:\\DeviceFile.gxc\"");
            sb.AppendLine(" -t\t Run Conformance tests. Conformance tests are not run with external tests.");
            sb.AppendLine(" -x\t External test file or directory.");
            sb.AppendLine(" -o\t Output test directory.");
            sb.AppendLine(" -s\t External Settings file. If external test file is given, this is ignored.");
            sb.AppendLine(" -c\t Closes application after tests are run (Never, Always, Success).");
            sb.AppendLine(" Note! The argument string that you pass can not have spaces or double-quotes in it.");
            sb.AppendLine("%userprofile%\\Desktop\\GXDLMSDirector.appref-ms \"C:\\DeviceFile.gxc -c Never\"");
            MessageBox.Show(sb.ToString());
        }

        private void It_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem it = sender as ToolStripMenuItem;
            it.Checked = !it.Checked;
            NewMnu_Click(null, null);
            if (it.Checked)
            {
                if (activeDC != null)
                {
                    foreach (ToolStripMenuItem mi in DataConcentratorsMnu.DropDownItems)
                    {
                        if (mi.Checked)
                        {
                            it.Click -= It_Click;
                            mi.Checked = false;
                            it.Click += It_Click;
                            break;
                        }
                    }
                }
                activeDC = it.Tag as IGXDataConcentrator;
                Properties.Settings.Default.ActiveDC = it.Text;
            }
            else
            {
                activeDC = null;
                Properties.Settings.Default.ActiveDC = "";
            }
            StatusLbl.Text = GetReadyText();
            UpdateDeviceUI(null, DeviceState.None);
            notificationsToolStripMenuItem.Enabled = mruManager.Enabled = RecentFilesMnu.Enabled = activeDC == null;
            CloneBtn.Enabled = AddObjectMenu.Enabled = NewBtn.Enabled = OpenBtn.Enabled = SaveBtn.Enabled = NewMnu.Enabled = SaveAsMnu.Enabled = SaveMnu.Enabled = OpenMnu.Enabled = activeDC == null;
            if (activeDC != null)
            {
                DeleteMnu.Enabled = DeleteBtn.Enabled = (activeDC.Actions & Actions.Remove) != 0;
                AddDeviceMnu.Enabled = (activeDC.Actions & Actions.Add) != 0;
                PropertiesMnu.Enabled = OptionsBtn.Enabled = (activeDC.Actions & Actions.Edit) != 0;
            }
            else
            {
                DeleteMnu.Enabled = DeleteBtn.Enabled = AddDeviceMnu.Enabled = PropertiesMnu.Enabled = OptionsBtn.Enabled = true;
            }
        }

        delegate void AddNotificationEventHanded(string data);

        /// <summary>
        /// Add new notification string.
        /// </summary>
        /// <param name="data">Data to add.</param>
        void OnAddNotification(string data)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new AddNotificationEventHanded(OnAddNotification), data);
            }
            else
            {
                try
                {
                    EventsView.AppendText(data);
                    EventsView.AppendText(Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }


        /// <summary>
        /// Client disconnects from sending events.
        /// </summary>
        private void Events_OnClientDisconnected(object sender, ConnectionEventArgs e)
        {
            OnAddNotification("Client disconnected: " + e.Info);
        }

        /// <summary>
        /// New client connects to send events.
        /// </summary>
        private void Events_OnClientConnected(object sender, ConnectionEventArgs e)
        {
            OnAddNotification("Client connected: " + e.Info);
        }

        private void ConverToString(StringBuilder sb, object value)
        {
            if (value is byte[])
            {
                sb.AppendLine(GXCommon.ToHex((byte[])value, true));
            }
            else if (value is object[])
            {
                sb.AppendLine("{");
                foreach (object it in (object[])value)
                {
                    ConverToString(sb, it);
                    if (sb.Length != 0)
                    {
                        sb.Length -= 2;
                    }
                    sb.Append(", ");
                }
                if (sb.Length != 0)
                {
                    sb.Length -= 2;
                }
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine(Convert.ToString(value));
            }
        }

        /// <summary>
        /// Meter sends event notification.
        /// </summary>
        private void Events_OnReceived(object sender, ReceiveEventArgs e)
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new ReceivedEventHandler(Events_OnReceived), sender, e);
            }
            else
            {
                if (e.Data is string)
                {
                    OnAddNotification(DateTime.Now.ToString() + Environment.NewLine + e.Data);
                }
                else if (NotificationHex.Checked)
                {
                    //Show as hex.
                    OnAddNotification(DateTime.Now.ToString() + Environment.NewLine + GXCommon.ToHex((byte[])e.Data, true));
                }
                else
                {
                    try
                    {
                        //Show as PDU.
                        eventsTranslator.PduOnly = NotificationPdu.Checked;
                        eventsData.Set((byte[])e.Data);
                        GXByteBuffer pdu = new GXByteBuffer();
                        InterfaceType type = GXDLMSTranslator.GetDlmsFraming(eventsData);
                        StringBuilder sb = new StringBuilder();
                        while (eventsTranslator.FindNextFrame(eventsData, pdu, type))
                        {
                            sb.Append(eventsTranslator.MessageToXml(eventsData));
                            pdu.Clear();
                        }
                        if (eventsData.Size == eventsData.Position)
                        {
                            eventsData.Clear();
                        }
                        if (AutoReset.Checked)
                        {
                            EventsView.ResetText();
                        }
                        if (NotificationTimeMnu.Checked)
                        {
                            OnAddNotification(DateTime.Now.ToString() + Environment.NewLine + sb.ToString());
                        }
                        else
                        {
                            OnAddNotification(sb.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        eventsData.Clear();
                        OnAddNotification(ex.Message);
                    }
                }
            }
        }

        private void Events_OnMediaStateChange(object sender, MediaStateEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MediaStateChangeEventHandler(Events_OnMediaStateChange), e);
            }
            else
            {
                if (e.State == MediaState.Open)
                {
                    OnAddNotification("Notifications listen started on " + (sender as GXNet).Protocol + " port " + (sender as GXNet).Port + ".");
                    StartNotifications.Visible = false;
                    StopNotifications.Visible = true;
                    NotificationsBtn.Checked = true;
                }
                else if (e.State == MediaState.Closed)
                {
                    OnAddNotification("Notifications listen closed.");
                    StartNotifications.Visible = true;
                    StopNotifications.Visible = false;
                    NotificationsBtn.Checked = false;
                }
            }
        }

        static void CheckUpdates(object data)
        {
            try
            {
                DateTime LastUpdateCheck = DateTime.MinValue;
                MainForm main = (MainForm)data;
                //Wait for a while before check updates.
                //Check new updates once a day.
                while (true)
                {
                    if (LastUpdateCheck.AddDays(1) < DateTime.Now)
                    {
                        LastUpdateCheck = DateTime.Now;
                        bool isConnected = true;
                        if (Environment.OSVersion.Platform != PlatformID.Unix)
                        {
                            Gurux.Win32.InternetConnectionState flags = Gurux.Win32.InternetConnectionState.INTERNET_CONNECTION_LAN | Gurux.Win32.InternetConnectionState.INTERNET_CONNECTION_CONFIGURED;
                            isConnected = Gurux.Win32.InternetGetConnectedState(ref flags, 0);
                        }
                        //Check if there are new app versions.
                        try
                        {
                            UpdateCheckInfo info = null;
                            //Check is Click once installed.
                            if (!Debugger.IsAttached && isConnected && ApplicationDeployment.IsNetworkDeployed)
                            {
                                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                                try
                                {
                                    info = ad.CheckForDetailedUpdate();
                                }
                                catch (DeploymentDownloadException dde)
                                {
                                    MessageBox.Show("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                                    return;
                                }
                                catch (InvalidDeploymentException ide)
                                {
                                    MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                                    return;
                                }
                                catch (InvalidOperationException ioe)
                                {
                                    MessageBox.Show("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                                    return;
                                }
                                if (info.UpdateAvailable)
                                {
                                    main.BeginInvoke(new CheckUpdatesEventHandler(OnNewAppVersion), new object[] { main });
                                }
                            }
                        }
                        catch (Exception Ex)
                        {
                            //Skip error.
                        }
                        //If there are updates available.
                        if (isConnected && GXManufacturerCollection.IsUpdatesAvailable())
                        {
                            main.BeginInvoke(new CheckUpdatesEventHandler(OnNewObisCodes), new object[] { main });
                            break;
                        }
                    }
                    //Wait for a day before next check.
                    System.Threading.Thread.Sleep(DateTime.Now.AddDays(1) - DateTime.Now);
                }
            }
            catch
            {
                //It's OK if this fails.
                //Wait for a day before next check.
                System.Threading.Thread.Sleep(DateTime.Now.AddDays(1) - DateTime.Now);
            }
        }

        static void OnNewObisCodes(MainForm form)
        {
            form.updateManufactureSettingsToolStripMenuItem.Visible = true;
        }

        static void OnNewAppVersion(MainForm form)
        {
            form.UpdateToLatestVersionMnu.Visible = true;
        }

        private void updateManufactureSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show(this, GXDLMSDirector.Properties.Resources.UpdateManufacturersOnlineTxt, GXDLMSDirector.Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
                {
                    GXManufacturerCollection.UpdateManufactureSettings();
                    Manufacturers = new GXManufacturerCollection();
                    GXManufacturerCollection.ReadManufacturerSettings(Manufacturers);
                }
                updateManufactureSettingsToolStripMenuItem.Visible = false;
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        void OnAsyncStateChange(object sender, GXAsyncWork work, object[] parameters, AsyncState state, string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new AsyncStateChangeEventHandler(OnAsyncStateChange), sender, work, parameters, state, text);
            }
            else
            {
                CancelBtn.Enabled = state == AsyncState.Start;
                if (state == AsyncState.Cancel)
                {
                    DisconnectMnu_Click(this, null);
                }
                ProgressBar.Visible = state == AsyncState.Start;
                if (state == AsyncState.Finish ||
                        state == AsyncState.Cancel)
                {
                    StatusLbl.Text = GetReadyText();
                }
            }
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            try
            {
                TransactionWork.Cancel();
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        void OnOpenMRUFile(string fileName)
        {
            try
            {
                LoadFile(fileName);
            }
            catch (Exception Ex)
            {
                mruManager.Remove(fileName);
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        delegate void GroupItemsEventHandler(bool bGroup, GXDLMSMeter refreshDevice);

        void GroupItems(bool bGroup, GXDLMSMeter refreshDevice)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new GroupItemsEventHandler(GroupItems), bGroup, refreshDevice);
            }
            else
            {
                if (refreshDevice != null)
                {
                    Dictionary<ObjectType, ListViewGroup> groups = new Dictionary<ObjectType, ListViewGroup>();
                    List<ListViewItem> items = new List<ListViewItem>();
                    TreeNode deviceNode = (TreeNode)ObjectTreeItems[refreshDevice];
                    ListViewGroup group = null;
                    foreach (GXDLMSObject it in refreshDevice.Objects)
                    {
                        if (bGroup)
                        {
                            if (!groups.ContainsKey(it.ObjectType))
                            {
                                group = new ListViewGroup(it.ObjectType.ToString(), it.ObjectType.ToString());
                                groups.Add(it.ObjectType, group);
                            }
                            else
                            {
                                group = groups[it.ObjectType];
                            }
                        }
                        items.Add(AddNode(it, deviceNode, group));
                    }
                    ObjectList.Groups.AddRange(groups.Values.ToArray());
                    ObjectList.Items.AddRange(items.ToArray());
                }
                else
                {
                    ObjectList.Groups.Clear();
                    ObjectList.Items.Clear();
                    SelectedListItems.Clear();
                    ObjectListItems.Clear();
                    ObjectTreeItems.Clear();
                    ObjectTreeItems[Devices] = this.ObjectTree.Nodes[0];
                    this.ObjectTree.Nodes[0].Nodes.Clear();
                    Dictionary<ObjectType, ListViewGroup> groups = new Dictionary<ObjectType, ListViewGroup>();
                    List<ListViewItem> items = new List<ListViewItem>();
                    foreach (GXDLMSMeter dev in Devices)
                    {
                        TreeNode deviceNode = AddDevice(dev, true, false);
                        ListViewGroup group = null;
                        foreach (GXDLMSObject it in dev.Objects)
                        {
                            if (bGroup)
                            {
                                if (!groups.ContainsKey(it.ObjectType))
                                {
                                    group = new ListViewGroup(it.ObjectType.ToString(), it.ObjectType.ToString());
                                    groups.Add(it.ObjectType, group);
                                }
                                else
                                {
                                    group = groups[it.ObjectType];
                                }
                            }
                            items.Add(AddNode(it, deviceNode, group));
                        }
                    }
                    ObjectList.Groups.AddRange(groups.Values.ToArray());
                    ObjectList.Items.AddRange(items.ToArray());
                }
            }
        }

        private void GroupsMnu_Click(object sender, EventArgs e)
        {
            GroupsMnu.Checked = !GroupsMnu.Checked;
            GroupItems(GroupsMnu.Checked, null);
        }

        private void ObjectList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                foreach (ListViewItem it in ObjectList.SelectedItems)
                {
                    RemoveObject(it.Tag);
                }
            }
        }

        private void ObjectList_Resize(object sender, EventArgs e)
        {
            try
            {
                DescriptionColumnHeader.Width = ObjectList.ClientSize.Width;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Show or hide object tree.
        /// </summary>
        private void ObjectTreeMnu_Click(object sender, EventArgs e)
        {
            ObjectTreeMnu.Checked = !ObjectTreeMnu.Checked;
            if (ObjectTreeMnu.Checked)
            {
                if (!this.tabControl1.TabPages.Contains(this.TreeView))
                {
                    this.tabControl1.TabPages.Add(this.TreeView);
                }
            }
            else if (this.tabControl1.TabPages.Contains(this.TreeView))
            {
                this.tabControl1.TabPages.Remove(this.TreeView);
            }

        }
        /// <summary>
        /// Show or hide object list.
        /// </summary>
        private void ObjectListMnu_Click(object sender, EventArgs e)
        {
            ObjectListMnu.Checked = !ObjectListMnu.Checked;
            if (ObjectListMnu.Checked)
            {
                if (!this.tabControl1.TabPages.Contains(this.ListView))
                {
                    this.tabControl1.TabPages.Add(this.ListView);
                }
            }
            else if (this.tabControl1.TabPages.Contains(this.ListView))
            {
                this.tabControl1.TabPages.Remove(this.ListView);
            }
        }

        private void ObjectList_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            try
            {
                this.ObjectTree.AfterSelect -= new System.Windows.Forms.TreeViewEventHandler(this.ObjectTree_AfterSelect);
                if (e.IsSelected)
                {
                    SelectedListItems.Add(e.ItemIndex, e.Item.Tag as GXDLMSObject);
                    if (SelectedListItems.Count == 1)
                    {
                        SelectItem(e.Item.Tag);
                    }
                    else
                    {
                        GXDLMSObjectCollection items = new GXDLMSObjectCollection();
                        items.AddRange(SelectedListItems.Values.ToArray());
                        SelectItem(items);
                    }
                    TreeNode node = ObjectTreeItems[e.Item.Tag] as TreeNode;
                    if (node != null)
                    {
                        ObjectTree.SelectedNode = node;
                        //  textBox1.Text = "on teste ici";
                    }
                }
                else
                {
                    int pos = SelectedListItems.IndexOfValue(e.Item.Tag as GXDLMSObject);
                    if (pos != -1)
                    {
                        SelectedListItems.RemoveAt(pos);
                    }
                    else
                    {
                        return;
                    }
                    if (SelectedListItems.Count == 0)
                    {
                        SelectItem(((GXDLMSObject)e.Item.Tag).Parent.Tag);
                    }
                    else if (SelectedListItems.Count == 1)
                    {
                        SelectItem(SelectedListItems.Values[0]);
                    }
                    else
                    {
                        GXDLMSObjectCollection items = new GXDLMSObjectCollection();
                        items.AddRange(SelectedListItems.Values.ToArray());
                        SelectItem(items);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                this.ObjectTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ObjectTree_AfterSelect);
            }
        }

        private void ObjectValueView_DoubleClick(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                object target = ObjectValueView.SelectedItems[0].Tag;
                TreeNode node = ObjectTreeItems[target] as TreeNode;
                ObjectTree.SelectedNode = node;
                //    textBox1.Text = "on teste cette methode";
            }
            else
            {
                if (ObjectValueView.SelectedItems.Count == 1)
                {
                    object target = ObjectValueView.SelectedItems[0].Tag;
                    ObjectList.SelectedItems.Clear();
                    ListViewItem item = ObjectListItems[target] as ListViewItem;
                    if (item != null)
                    {
                        item.Selected = true;
                    }
                }
            }
        }

        /// <summary>
        /// Show library versions.
        /// </summary>
        private void LibraryVersionsMenu_Click(object sender, EventArgs e)
        {
            try
            {
                LibraryVersionsDlg dlg = new LibraryVersionsDlg();
                dlg.ShowDialog(this);
            }
            catch (Exception Ex)
            {
                GXCommon.ShowError(Ex);
            }
        }

        /// <summary>
        /// Force read.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ForceReadMnu_Click(object sender, EventArgs e)
        {
            ForceRefreshBtn.Checked = ForceReadMnu.Checked = !ForceReadMnu.Checked;
        }

        /// <summary>
        /// Start listen notifications.
        /// </summary>
        private void StartNotifications_Click(object sender, EventArgs e)
        {
            try
            {
                events.Open();
            }
            catch (Exception Ex)
            {
                GXCommon.ShowError(Ex);
            }
        }

        /// <summary>
        /// Stop listen notifications.
        /// </summary>
        private void StopNotifications_Click(object sender, EventArgs e)
        {
            try
            {
                events.Close();
            }
            catch (Exception Ex)
            {
                GXCommon.ShowError(Ex);
            }
        }

        private void NotificationsBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (events.IsOpen)
                {
                    events.Close();
                }
                else
                {
                    events.Open();
                }
            }
            catch (Exception Ex)
            {
                GXCommon.ShowError(Ex);
            }
        }

        private void ClearNotifications_Click(object sender, EventArgs e)
        {
            EventsView.ResetText();
        }

        private void AutoReset_Click(object sender, EventArgs e)
        {
            AutoReset.Checked = !AutoReset.Checked;
        }

        private void UpdateTrace(byte level)
        {
            traceLevel = level;
            panel1.Visible = level != 0 || EventsView.Visible || ConformanceTests.Visible;
            TraceView.Visible = level != 0;
            noneToolStripMenuItem.Checked = level == 0;
            hexToolStripMenuItem.Checked = level == 1;
            xmlToolStripMenuItem.Checked = level == 2;
            pDUToolStripMenuItem.Checked = level == 3;
            //Show data as pdu.
            traceTranslator.PduOnly = level == 3;

            TraceCommentsMenu.Enabled = level > 1;
        }

        /// <summary>
        /// Hide trace.
        /// </summary>
        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateTrace(0);
        }

        /// <summary>
        /// Show trace in Hex Mode.
        /// </summary>
        private void hexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateTrace(1);
        }

        /// <summary>
        /// Show trace in PDU Mode.
        /// </summary>
        private void pDUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateTrace(3);
        }

        /// <summary>
        /// Show trace in Xml Mode.
        /// </summary>
        private void xmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateTrace(2);
        }

        /// <summary>
        /// Show settings.
        /// </summary>
        private void settingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                GXSettingsDlg dlg = new GXSettingsDlg(events);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Properties.Settings.Default.EventsSettings = events.Settings;
                }
            }
            catch (Exception Ex)
            {
                GXCommon.ShowError(Ex);
            }

        }

        private static GXDLMSMeter GetDevice(TreeNode node)
        {
            GXDLMSMeter dev = null;
            if (node != null)
            {
                if (node.Tag is GXDLMSDeviceCollection)
                {
                    dev = null;
                }
                if (node.Tag is GXDLMSMeter)
                {
                    dev = node.Tag as GXDLMSMeter;
                }
                else if (node.Tag is GXDLMSObject)
                {
                    dev = GetDevice(node.Tag as GXDLMSObject);
                }
                else if (node.Parent != null)
                {
                    dev = (GXDLMSMeter)node.Parent.Tag;
                }
            }
            return dev;
        }

        private void ObjectTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            // textBox1.Text = "  ----->on entre dans le bifore selected  ";
            GXDLMSMeter oldDev = GetDevice(ObjectTree.SelectedNode);
            GXDLMSMeter newDev = GetDevice(e.Node);
            if (oldDev != newDev)
            {
                eventsTranslator.Clear();
                if (newDev != null)
                {
                    traceTranslator.Security = newDev.Security;
                    traceTranslator.SystemTitle = GXCommon.HexToBytes(newDev.SystemTitle);
                    traceTranslator.BlockCipherKey = GXCommon.HexToBytes(newDev.BlockCipherKey);
                    traceTranslator.AuthenticationKey = GXCommon.HexToBytes(newDev.AuthenticationKey);
                    traceTranslator.InvocationCounter = newDev.InvocationCounter;
                    DedicatedKeyTb.Text = GXCommon.ToHex(GXCommon.HexToBytes(newDev.DedicatedKey));
                    AuthenticationTb.Text = newDev.Authentication.ToString();
                    if (newDev.PreEstablished)
                    {
                        traceTranslator.ServerSystemTitle = GXCommon.HexToBytes(newDev.ServerSystemTitle);
                    }
                    else
                    {
                        GXDLMSDevice d = newDev as GXDLMSDevice;
                        if (d != null)
                        {
                            traceTranslator.ServerSystemTitle = d.Comm.client.ServerSystemTitle;
                        }
                    }
                    if (newDev.Security != Security.None || newDev.Authentication == Authentication.HighGMAC ||
                        newDev.Authentication == Authentication.HighECDSA)
                    {
                        ClientSystemTitleTb.Text = GXCommon.ToHex(GXCommon.HexToBytes(newDev.SystemTitle));
                        ServerSystemTitleTb.Text = GXCommon.ToHex(traceTranslator.ServerSystemTitle);
                        SecurityTb.Text = newDev.Security.ToString();
                        AuthenticationKeyTb.Text = GXCommon.ToHex(GXCommon.HexToBytes(newDev.AuthenticationKey));
                        BlockCipherKeyTb.Text = GXCommon.ToHex(GXCommon.HexToBytes(newDev.BlockCipherKey));
                    }
                    else
                    {
                        AuthenticationKeyTb.Text = BlockCipherKeyTb.Text = SecurityTb.Text = ClientSystemTitleTb.Text = ServerSystemTitleTb.Text = "";
                    }
                    NetworkIDTb.Text = newDev.NetworkId.ToString();
                    PhysicalDeviceAddressTb.Text = GXCommon.ToHex(GXCommon.HexToBytes(newDev.PhysicalDeviceAddress));
                }
            }
        }

        private void UpdateNotification(byte level)
        {
            notificationLevel = level;
            panel1.Visible = level != 0 || TraceView.Visible || ConformanceTests.Visible;
            splitter3.Visible = EventsView.Visible = level != 0;
            NotificationNone.Checked = level == 0;
            NotificationHex.Checked = level == 1;
            NotificationXml.Checked = level == 2;
            NotificationPdu.Checked = level == 3;
            //Show data as pdu.
            eventsTranslator.PduOnly = level == 3;
            if (level == 0 && !ConformanceTests.Visible)
            {
                TraceView.Dock = DockStyle.Fill;
            }
            else
            {
                TraceView.Dock = DockStyle.Left;
            }
            NotificationsCommentsMenu.Enabled = level > 1;
        }


        private void NotificationNone_Click(object sender, EventArgs e)
        {
            UpdateNotification(0);
        }

        private void NotificationHex_Click(object sender, EventArgs e)
        {
            UpdateNotification(1);
        }

        private void NotificationXml_Click(object sender, EventArgs e)
        {
            UpdateNotification(2);

        }

        private void NotificationPdu_Click(object sender, EventArgs e)
        {
            UpdateNotification(3);
        }

        /// <summary>
        /// OBIS code to search.
        /// </summary>
        string search = null;

        private bool FindTreeNode(TreeNodeCollection nodes, string text, ref TreeNode first)
        {
            foreach (TreeNode it in nodes)
            {
                if (first == null)
                {
                    if (it == ObjectTree.SelectedNode)
                    {
                        first = it;
                    }
                }
                else if (it.Tag is GXDLMSObject && (it.Tag as GXDLMSObject).LogicalName == search)
                {
                    ObjectTree.SelectedNode = it;
                    return true;
                }
                if (it.Nodes.Count != 0)
                {
                    if (FindTreeNode(it.Nodes, text, ref first))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Find OBIS code from the tree view.
        /// </summary>
        /// <param name="nodes">List of OBIS codes to search.</param>
        /// <param name="text">OBIS code to search</param>
        /// <param name="first">If selected item found.</param>
        /// <returns>Is OBIS code found.</returns>
        private bool FindTreeNode(TreeNodeCollection nodes, string text, ref bool first)
        {
            TreeNode node = ObjectTree.SelectedNode;
            foreach (TreeNode it in nodes)
            {
                if (!first)
                {
                    first = it == node;
                }
                else if (it.Tag is GXDLMSObject && (it.Tag as GXDLMSObject).LogicalName == search)
                {
                    ObjectTree.SelectedNode = it;
                    return true;
                }
                if (it.Nodes.Count != 0)
                {
                    if (FindTreeNode(it.Nodes, text, ref first))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Find next OBIS code from the tree view.
        /// </summary>
        private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                bool first = false;
                if (!FindTreeNode(ObjectTree.Nodes, search, ref first))
                {
                    first = true;
                    if (!FindTreeNode(ObjectTree.Nodes, search, ref first))
                    {
                        throw new Exception("OBIS code '" + search + "' not found!");
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Find OBIS code from the tree view.
        /// </summary>
        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                GXFindParameters p = new GXFindParameters();
                if (GXDlmsUi.Find(this, p))
                {
                    search = p.ObisCode;
                    bool tmp = string.IsNullOrEmpty(search);
                    findNextToolStripMenuItem.Enabled = !tmp;
                    if (!tmp)
                    {
                        findNextToolStripMenuItem_Click(null, null);
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Clone selected device.
        /// </summary>
        private void CloneBtn_Click(object sender, EventArgs e)
        {
            try
            {
                object tag = ObjectTree.SelectedNode.Tag;
                GXDLMSDevice dev = null;
                if (tag is GXDLMSDevice)
                {
                    dev = tag as GXDLMSDevice;
                }
                else if (tag is GXDLMSObject)
                {
                    dev = GetDevice(tag as GXDLMSObject);
                }
                if (dev != null)
                {
                    //Create clone from original items.
                    using (MemoryStream ms = new MemoryStream())
                    {
                        List<Type> types = new List<Type>(GXDLMSClient.GetObjectTypes());
                        types.Add(typeof(GXDLMSAttributeSettings));
                        types.Add(typeof(GXDLMSAttribute));
                        XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                        XmlAttributes attribs = new XmlAttributes();
                        attribs.XmlIgnore = true;
                        overrides.Add(typeof(GXDLMSDevice), "ObsoleteObjects", attribs);
                        overrides.Add(typeof(GXDLMSAttributeSettings), attribs);
                        XmlSerializer x = new XmlSerializer(typeof(GXDLMSDevice), overrides, types.ToArray(), null, "Gurux1");
                        using (TextWriter writer = new StreamWriter(ms))
                        {
                            x.Serialize(writer, dev);
                            ms.Position = 0;
                            using (XmlReader reader = XmlReader.Create(ms))
                            {
                                dev = (GXDLMSDevice)x.Deserialize(reader);
                            }
                        }
                        ms.Close();
                        dev.Name = "";
                        AddDevice(dev);
                    }
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Log as hex.
        /// </summary>
        private void LogHexBtn_Click(object sender, EventArgs e)
        {
            LogHexBtn.Checked = !LogHexBtn.Checked;
            Properties.Settings.Default.Log = LogHexBtn.Checked ? 1 : 0;
            if (LogXmlBtn.Checked)
            {
                Properties.Settings.Default.Log |= 2;
            }
            GXLogWriter.LogLevel = Properties.Settings.Default.Log;
            LogCommentsMenu.Enabled = LogXmlBtn.Checked;
        }

        /// <summary>
        /// Log as XML.
        /// </summary>
        private void LogXmlBtn_Click(object sender, EventArgs e)
        {
            LogXmlBtn.Checked = !LogXmlBtn.Checked;
            Properties.Settings.Default.Log = LogHexBtn.Checked ? 1 : 0;
            if (LogXmlBtn.Checked)
            {
                Properties.Settings.Default.Log |= 2;
            }
            GXLogWriter.LogLevel = Properties.Settings.Default.Log;
            LogCommentsMenu.Enabled = LogXmlBtn.Checked;
        }

        /// <summary>
        /// Save device as template.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAsTemplateBtn_Click(object sender, EventArgs e)
        {
            try
            {
                object tag = ObjectTree.SelectedNode.Tag;
                if (tag != null)
                {
                    GXDLMSDevice dev = null;
                    if (tag is GXDLMSDevice)
                    {
                        dev = tag as GXDLMSDevice;
                    }
                    else if (tag is GXDLMSObject)
                    {
                        dev = GetDevice(tag as GXDLMSObject);
                    }
                    else if (tag is GXDLMSObjectCollection)
                    {
                        dev = (tag as GXDLMSObjectCollection).Tag as GXDLMSDevice;
                    }
                    if (dev != null)
                    {
                        GXManufacturer man = Manufacturers.FindByIdentification(dev.Manufacturer);
                        man.ObisCodes.Clear();
                        foreach (GXDLMSObject it in dev.Objects)
                        {
                            GXObisCode c = new GXObisCode(it.LogicalName, it.ObjectType, null);
                            c.Append = true;
                            man.ObisCodes.Add(c);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Show help not available message.
        /// </summary>
        /// <param name="hevent">A HelpEventArgs that contains the event data.</param>
        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            try
            {
                // Get the control where the user clicked
                Point lpe = menuStrip1.PointToClient(hevent.MousePos);
                Control ctl = this.GetChildAtPoint(this.PointToClient(hevent.MousePos));
                string str = "https://www.gurux.fi/index.php?q=GXDLMSDirectorHelp";
                if (ctl == toolStrip1 || ctl == menuStrip1)
                {
                    str = "https://www.gurux.fi/GXDLMSDirector.Menu";
                }
                else if (ctl == tabControl2 && tabControl2.SelectedTab == tabPage1 && SelectedView != null)
                {
                    GXDLMSViewAttribute[] att = (GXDLMSViewAttribute[])SelectedView.GetType().GetCustomAttributes(typeof(GXDLMSViewAttribute), true);
                    str = "https://www.gurux.fi/index.php?q=" + att[0].DLMSType.ToString();
                }
                else if (ctl == ObjectValueView && ObjectValueView.Items.Count != 0)
                {
                    str = "https://www.gurux.fi/index.php?q=" + ObjectValueView.Items[0].Tag.GetType();
                }
                else if (ctl == panel1)
                {
                    ctl = panel1.GetChildAtPoint(panel1.PointToClient(hevent.MousePos));
                    if (ctl == ConformanceTests)
                    {
                        str = "https://www.gurux.fi/index.php?q=GXDLMSDirector.ConformanceTest";
                    }
                }
                // Show online help.
                Process.Start(str);
                // Set flag to show that the Help event as been handled
                hevent.Handled = true;
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Show result of conformance test.
        /// </summary>
        private void ShowReportBtn_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedListViewItemCollection target;
                if (IsConformanceTestSelected(sender))
                {
                    target = ConformanceTests.SelectedItems;
                }
                else
                {
                    target = ConformanceHistoryTests.SelectedItems;
                }
                if (target.Count == 1)
                {
                    ListViewItem li = target[0];
                    string path = Path.Combine((li.Tag as GXConformanceTest).Results, "Results.html");
                    Process.Start(path);
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Show log of conformance test.
        /// </summary>
        private void ShowLogBtn_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedListViewItemCollection target;
                if (IsConformanceTestSelected(sender))
                {
                    target = ConformanceTests.SelectedItems;
                }
                else
                {
                    target = ConformanceHistoryTests.SelectedItems;
                }
                if (target.Count == 1)
                {
                    ListViewItem li = target[0];
                    string path = Path.Combine((li.Tag as GXConformanceTest).Results, "Trace.txt");
                    Process.Start(path);
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        void ConformanceExecute(object sender, GXAsyncWork work, object[] parameters)
        {
            GXConformanceSettings p = (GXConformanceSettings)parameters[1];
            if (p.ConcurrentTesting)
            {
                List<GXConformanceTest> tests = (List<GXConformanceTest>)parameters[0];
                List<GXConformanceTest> alltests = (List<GXConformanceTest>)parameters[2];
                GXConformanceParameters cp = new GXConformanceParameters();
                cp.numberOfTasks = tests.Count;
                cp.finished = new ManualResetEvent(false);
                foreach (GXConformanceTest it in tests)
                {
                    Thread t = new Thread(() =>
                    {
                        List<GXConformanceTest> tmp = new List<GXConformanceTest>();
                        tmp.Add(it);
                        GXConformanceTests.ReadXmlMeter(new object[] { tmp, p, alltests, cp });
                    }
                    );
                    t.IsBackground = true;
                    t.Start();
                }
                cp.finished.WaitOne();
            }
            else
            {
                GXConformanceTests.ReadXmlMeter(parameters);
            }
        }

        void ConformanceError(object sender, Exception ex)
        {
            MessageBox.Show(this, ex.Message);
        }

        void ConformanceStateChange(object sender, GXAsyncWork work, object[] parameters, AsyncState state, string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new AsyncStateChangeEventHandler(ConformanceStateChange), sender, work, parameters, state, text);
            }
            else
            {
                CancelBtn.Enabled = state == AsyncState.Start;
                ProgressBar.Visible = state == AsyncState.Start;
                GXConformanceTests.Continue = state != AsyncState.Cancel;
                if (state == AsyncState.Finish ||
                        state == AsyncState.Cancel)
                {
                    StatusLbl.Text = GetReadyText();
                    if (state == AsyncState.Finish)
                    {
                        if (parameters.Length == 3)
                        {
                            if (parameters[1] is GXConformanceSettings)
                            {
                                bool close = (parameters[1] as GXConformanceSettings).CloseApplication == CloseApp.Always;
                                //Check test status.
                                if ((parameters[1] as GXConformanceSettings).CloseApplication == CloseApp.Success)
                                {
                                    close = true;
                                    foreach (GXConformanceTest it in parameters[2] as List<GXConformanceTest>)
                                    {
                                        if (it.ErrorLevel != 0)
                                        {
                                            close = false;
                                            break;
                                        }
                                    }
                                }
                                if (close)
                                {
                                    this.Close();
                                    return;
                                }
                            }
                        }
                        MessageBox.Show(this, "Gurux Conformance Tests end.");
                    }
                }
                else
                {
                    StatusLbl.Text = "Running Gurux Conformance Tests.";
                }
            }
        }


        /// <summary>
        /// Conformance test is finnished.
        /// </summary>
        /// <param name="sender">Executed conformance test.</param>
        void OnConformanceReady(GXConformanceTest sender)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ConformanceReadyEvent(OnConformanceReady), sender);
            }
            else
            {
                foreach (ListViewItem li in ConformanceTests.Items)
                {
                    if (li.Tag == sender)
                    {
                        ListViewItem tmp = new ListViewItem(Path.GetFileName((li.Tag as GXConformanceTest).Results));
                        tmp.SubItems.Add("");
                        tmp.Tag = sender;
                        ConformanceHistoryTests.Items.Insert(0, tmp);
                        li.ImageIndex = sender.ErrorLevel;
                        li.Selected = true;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Show failed conformance test.
        /// </summary>
        /// <param name="sender">Executed conformance test.</param>
        /// <param name="e">Occurred exception.</param>
        public void OnConformanceError(GXConformanceTest sender, Exception e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ConformanceErrorEvent(OnConformanceError), sender, e);
            }
            else
            {
                try
                {
                    if (traceLevel != 0)
                    {
                        TraceView.AppendText(e.ToString());
                    }
                    foreach (ListViewItem li in ConformanceTests.Items)
                    {
                        if (li.Tag == sender)
                        {
                            li.SubItems[1].Text = e.Message;
                            ListViewItem tmp = new ListViewItem(Path.GetFileName((li.Tag as GXConformanceTest).Results));
                            tmp.SubItems.Add("");
                            tmp.Tag = sender;
                            ConformanceHistoryTests.Items.Insert(0, tmp);
                            li.ImageIndex = sender.ErrorLevel;
                            li.Selected = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Show conformance test trace.
        /// </summary>
        /// <param name="sender">Executed conformance test.</param>
        /// <param name="data"></param>
        public void OnConformanceTrace(GXConformanceTest sender, string data)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ConformanceTraceEvent(OnConformanceTrace), sender, data);
            }
            else
            {
                try
                {
                    if (traceLevel != 0)
                    {
                        TraceView.AppendText(data);
                    }
                    using (FileStream fs = File.Open(Path.Combine(sender.Results, "Values.txt"), FileMode.Append))
                    {
                        using (TextWriter writer = new StreamWriter(fs))
                        {
                            writer.Write(data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }

        private bool RunConformanceTest(GXConformanceSettings settings, GXDLMSMeterCollection devices, bool showDlg, string testResults)
        {
            if (showDlg)
            {
                GXConformanceDlg dlg = new GXConformanceDlg(settings);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }
            }
            TraceView.ResetText();
            ConformanceTests.Items.Clear();
            GXConformanceTests.ValidateTests(settings);

            List<GXConformanceTest> tests = new List<GXConformanceTest>();
            if (testResults == null)
            {
                string path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
                if (!Directory.Exists(path2))
                {
                    Directory.CreateDirectory(path2);
                }
                testResults = Path.Combine(path2, "TestResults");
            }
            if (!Directory.Exists(testResults))
            {
                Directory.CreateDirectory(testResults);
            }
            string[] list = Directory.GetDirectories(testResults);
            int testcount = 0;
            foreach (GXDLMSDevice it in devices)
            {
                if (it.Media.IsOpen)
                {
                    //Conformance tests will close the connection.
                    it.UpdateStatus(DeviceState.Initialized);
                    UpdateDeviceUI(it, DeviceState.Initialized);
                }
                testcount += it.Objects.Count;
                GXConformanceTest t = new GXConformanceTest() { Device = it };
                t.OnReady = OnConformanceReady;
                t.OnError = OnConformanceError;
                t.OnTrace = OnConformanceTrace;
                t.OnProgress = OnProgress;
                t.Results = Path.Combine(testResults, it.Name);
                if (Directory.Exists(t.Results))
                {
                    t.Results = Path.Combine(testResults, it.Name + "_" + DateTime.Now.ToString("yyyy-MM-dd hh_mm_ss"));
                }
                Directory.CreateDirectory(t.Results);
                using (Stream stream = File.Open(Path.Combine(t.Results, "Results.html"), FileMode.Create))
                {

                }
                using (Stream stream = File.Open(Path.Combine(t.Results, "Trace.txt"), FileMode.Create))
                {

                }
                using (Stream stream = File.Open(Path.Combine(t.Results, "Values.txt"), FileMode.Create))
                {

                }
                using (Stream stream = File.Open(Path.Combine(t.Results, "settings.xml"), FileMode.Create))
                {
                    XmlSerializer x = new XmlSerializer(typeof(GXConformanceSettings));
                    using (TextWriter writer = new StreamWriter(stream))
                    {
                        x.Serialize(writer, settings);
                        writer.Flush();
                        writer.Close();
                    }
                }
                GXDLMSMeterCollection devs = new GXDLMSMeterCollection();
                devs.Add(GXConformanceTests.CloneDevice(it));
                Save(Path.Combine(t.Results, "device.gxc"), devs);
                tests.Add(t);
                ListViewItem li = ConformanceTests.Items.Add(it.Name);
                li.SubItems.Add("");
                li.Tag = t;
            }
            TraceView.ResetText();
            ProgressBar.Value = 0;
            ProgressBar.Maximum = testcount;
            GXConformanceTests.Continue = true;
            List<GXConformanceTest> alltests = new List<GXConformanceTest>();
            alltests.AddRange(tests);
            TransactionWork = new GXAsyncWork(this, ConformanceStateChange, ConformanceExecute, ConformanceError, null, new object[] { tests, settings, alltests });
            if (showDlg && settings.WarningBeforeStart)
            {
                DialogResult ret = MessageBox.Show(this, Properties.Resources.CTTWarning, Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (ret != DialogResult.Yes)
                {
                    return false;
                }
            }
            TransactionWork.Start();
            return true;
        }

        /// <summary>
        /// Show or hide conformance tests.
        /// </summary>
        private void conformanceTestsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            splitter4.Visible = conformanceTestsToolStripMenuItem1.Checked = !conformanceTestsToolStripMenuItem1.Checked;
            panel1.Visible = EventsView.Visible || TraceView.Visible || conformanceTestsToolStripMenuItem1.Checked;
            ConformanceTests.Visible = conformanceTestsToolStripMenuItem1.Checked;
            if (!conformanceTestsToolStripMenuItem1.Checked)
            {
                if (EventsView.Visible)
                {
                    EventsView.Dock = DockStyle.Fill;
                }
                else if (TraceView.Visible)
                {
                    TraceView.Dock = DockStyle.Fill;
                }
            }
            else
            {
                ConformanceTests.Dock = DockStyle.Fill;
                TraceView.Dock = EventsView.Dock = DockStyle.Left;
            }
        }

        private void contextMenuStrip2_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ContextMenuStrip senderItem = (ContextMenuStrip)sender;
            var ownerItem = senderItem.OwnerItem;
            RunMnu.Enabled = Devices.Count != 0 && (TransactionWork == null || !TransactionWork.IsRunning);
            ShowDurationsMnu.Enabled = OpenContainingFolderBtn.Enabled = showValuesBtn.Enabled = ShowLogBtn.Enabled = ShowReportBtn.Enabled = ConformanceTests.SelectedItems.Count != 0;
        }

        /// <summary>
        /// Show read values in Conformance Test Tool.
        /// </summary>
        private void showValuesBtn_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedListViewItemCollection target;
                if (IsConformanceTestSelected(sender))
                {
                    target = ConformanceTests.SelectedItems;
                }
                else
                {
                    target = ConformanceHistoryTests.SelectedItems;
                }
                if (target.Count == 1)
                {
                    ListViewItem li = target[0];
                    string path = Path.Combine((li.Tag as GXConformanceTest).Results, "Values.txt");
                    Process.Start(path);
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        private bool IsConformanceTestSelected(object sender)
        {
            if (sender == ConformanceTests)
            {
                return true;
            }
            if (sender == ConformanceHistoryTests)
            {
                return false;
            }
            ToolStripMenuItem t = (ToolStripMenuItem)sender;
            return t.Owner != ConformanceHistoryMenu;
        }

        /// <summary>
        /// Open containing folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenContainingFolderBtn_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedListViewItemCollection target;
                if (IsConformanceTestSelected(sender))
                {
                    target = ConformanceTests.SelectedItems;
                }
                else
                {
                    target = ConformanceHistoryTests.SelectedItems;
                }
                if (target.Count == 1)
                {
                    ListViewItem li = target[0];
                    Process.Start((li.Tag as GXConformanceTest).Results);
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Clear trace.
        /// </summary>
        private void clearTraceMnu_Click(object sender, EventArgs e)
        {
            TraceView.ResetText();
        }

        private void CopyTrace_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(TraceView.Text);
            }
            catch (Exception ex)
            {
                Error.ShowError(this, ex);
            }
        }

        private void NotificationCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(EventsView.Text);
            }
            catch (Exception ex)
            {
                Error.ShowError(this, ex);
            }
        }

        /// <summary>
        /// Are Log comments shown.
        /// </summary>
        private void LogCommentsMenu_Click(object sender, EventArgs e)
        {
            LogCommentsMenu.Checked = !LogCommentsMenu.Checked;
            eventsTranslator.Comments = LogCommentsMenu.Checked;
        }

        /// <summary>
        /// Are Trace comments shown.
        /// </summary>
        private void TraceCommentsMenu_Click(object sender, EventArgs e)
        {
            TraceCommentsMenu.Checked = !TraceCommentsMenu.Checked;
            traceTranslator.Comments = TraceCommentsMenu.Checked;
        }

        /// <summary>
        /// Are Notification comments shown.
        /// </summary>
        private void NotificationsCommentsMenu_Click(object sender, EventArgs e)
        {
            NotificationsCommentsMenu.Checked = !NotificationsCommentsMenu.Checked;
            eventsTranslator.Comments = NotificationsCommentsMenu.Checked;
        }

        /// <summary>
        /// Add new object to the device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddObjectMenu_Click(object sender, EventArgs e)
        {
            try
            {
                ObjectType ot = ObjectType.None;
                object tag = ObjectTree.SelectedNode.Tag;
                GXDLMSDevice dev = null;
                if (tag is GXDLMSDevice)
                {
                    dev = tag as GXDLMSDevice;
                }
                else if (tag is GXDLMSObject)
                {
                    ot = (tag as GXDLMSObject).ObjectType;
                    dev = GetDevice(tag as GXDLMSObject);
                }
                else if (ObjectTree.SelectedNode.Parent != null)
                {
                    dev = (GXDLMSDevice)ObjectTree.SelectedNode.Parent.Tag;
                }
                if (dev != null)
                {
                    GXObisCodeCollection collection = dev.Manufacturers.FindByIdentification(dev.Manufacturer).ObisCodes;
                    GXObisCode item = new GXObisCode();
                    item.ObjectType = ot;
                    OBISCodeForm dlg = new OBISCodeForm(new GXDLMSConverter(dev.Standard), dev.Objects, collection, item);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        GXDLMSObject obj = GXDLMSClient.CreateObject(item.ObjectType);
                        obj.Description = item.Description;
                        UpdateAttributes(obj, item);
                        dev.Objects.Add(obj);
                        TreeNode deviceNode = (TreeNode)ObjectTreeItems[dev];
                        ListViewGroup group = null;
                        foreach (ListViewGroup g in ObjectList.Groups)
                        {
                            if (g.ToString() == obj.ObjectType.ToString())
                            {
                                group = g;
                                break;
                            }
                        }
                        if (group == null)
                        {
                            group = new ListViewGroup(obj.ObjectType.ToString(), obj.ObjectType.ToString());
                            ObjectList.Groups.Add(group);
                        }
                        ListViewItem li = AddNode(obj, deviceNode, group);
                        ObjectList.Items.Add(li);
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void ErrorsView_DoubleClick(object sender, EventArgs e)
        {
            if (ErrorsView.SelectedItems.Count == 1)
            {
                object target = ErrorsView.SelectedItems[0].Tag;
                TreeNode node = ObjectTreeItems[target] as TreeNode;
                ObjectTree.SelectedNode = node;
            }
        }

        private void LogTimeMnu_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.LogTime = LogTimeMnu.Checked = !LogTimeMnu.Checked;
        }

        private void TraceTimeMnu_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.TraceTime = TraceTimeMnu.Checked = !TraceTimeMnu.Checked;
        }

        private void NotificationTimeMnu_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.NotificationTime = NotificationTimeMnu.Checked = !NotificationTimeMnu.Checked;
        }

        private void LogDuration_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.LogDuration = LogDuration.Checked = !LogDuration.Checked;
        }

        private void TraceDuration_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.TraceDuration = TraceDuration.Checked = !TraceDuration.Checked;
        }

        /// <summary>
        /// Show Conformance Test Tool conformances.
        /// </summary>
        private void ShowDurationsMnu_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedListViewItemCollection target;
                if (IsConformanceTestSelected(sender))
                {
                    target = ConformanceTests.SelectedItems;
                }
                else
                {
                    target = ConformanceHistoryTests.SelectedItems;
                }
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                foreach (ListViewItem li in target)
                {
                    GXConformanceTest ct = li.Tag as GXConformanceTest;
                    string path = Path.Combine(ct.Results, "Durations.txt");
                    if (File.Exists(path))
                    {
                        string name;
                        if (ct.Device == null)
                        {
                            name = Path.GetFileName(ct.Results);
                        }
                        else
                        {
                            name = ct.Device.Name;
                        }
                        values.Add(new KeyValuePair<string, string>(name, path));
                    }
                }
                if (values.Count != 0)
                {
                    GXDurations dlg = new GXDurations(values);
                    dlg.Show(this);
                }
                else
                {
                    throw new Exception(Properties.Resources.InvalidDurationFile);
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Run all conformance tests.
        /// </summary>
        private void RunAllTestsMnu_Click(object sender, EventArgs e)
        {
            try
            {
                if (Devices.Count == 0)
                {
                    throw new Exception("No devices to test.");
                }
                GXDLMSMeterCollection devices = new GXDLMSMeterCollection();
                devices.AddRange(Devices);
                GXConformanceSettings settings;
                XmlSerializer x = new XmlSerializer(typeof(GXConformanceSettings));
                if (Properties.Settings.Default.ConformanceSettings == "")
                {
                    settings = new GXConformanceSettings();
                }
                else
                {
                    try
                    {
                        using (StringReader reader = new StringReader(Properties.Settings.Default.ConformanceSettings))
                        {
                            settings = (GXConformanceSettings)x.Deserialize(reader);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        settings = new GXConformanceSettings();
                    }
                }
                if (RunConformanceTest(settings, devices, true, null))
                {
                    try
                    {
                        using (StringWriter writer = new StringWriter())
                        {
                            x.Serialize(writer, settings);
                            Properties.Settings.Default.ConformanceSettings = writer.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        Properties.Settings.Default.ConformanceSettings = "";
                    }
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Run selected conformance tests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunSelectedTestsMnu_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedListViewItemCollection target;
                target = ConformanceTests.SelectedItems;
                GXDLMSMeterCollection devices = new GXDLMSMeterCollection();
                foreach (ListViewItem it in target)
                {
                    devices.Add((it.Tag as GXConformanceTest).Device);
                }
                if (devices.Count == 0)
                {
                    throw new Exception("No devices selected.");
                }
                GXConformanceSettings settings;
                XmlSerializer x = new XmlSerializer(typeof(GXConformanceSettings));
                if (Properties.Settings.Default.ConformanceSettings == "")
                {
                    settings = new GXConformanceSettings();
                }
                else
                {
                    try
                    {
                        using (StringReader reader = new StringReader(Properties.Settings.Default.ConformanceSettings))
                        {
                            settings = (GXConformanceSettings)x.Deserialize(reader);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        settings = new GXConformanceSettings();
                    }
                }
                if (RunConformanceTest(settings, devices, true, null))
                {
                    try
                    {
                        using (StringWriter writer = new StringWriter())
                        {
                            x.Serialize(writer, settings);
                            Properties.Settings.Default.ConformanceSettings = writer.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        Properties.Settings.Default.ConformanceSettings = "";
                    }
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Run failed conformance test.
        /// </summary>
        private void RunFailedTestsMnu_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedListViewItemCollection target;
                if (IsConformanceTestSelected(sender))
                {
                    target = ConformanceTests.SelectedItems;
                }
                else
                {
                    target = ConformanceHistoryTests.SelectedItems;
                }
                GXDLMSMeterCollection devices = new GXDLMSMeterCollection();
                foreach (ListViewItem it in target)
                {
                    if ((it.Tag as GXConformanceTest).Exception != null)
                    {
                        devices.Add((it.Tag as GXConformanceTest).Device);
                    }
                }
                if (devices.Count == 0)
                {
                    throw new Exception("No devices selected.");
                }
                GXConformanceSettings settings;
                XmlSerializer x = new XmlSerializer(typeof(GXConformanceSettings));
                if (Properties.Settings.Default.ConformanceSettings == "")
                {
                    settings = new GXConformanceSettings();
                }
                else
                {
                    try
                    {
                        using (StringReader reader = new StringReader(Properties.Settings.Default.ConformanceSettings))
                        {
                            settings = (GXConformanceSettings)x.Deserialize(reader);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        settings = new GXConformanceSettings();
                    }
                }
                if (RunConformanceTest(settings, devices, true, null))
                {
                    try
                    {
                        using (StringWriter writer = new StringWriter())
                        {
                            x.Serialize(writer, settings);
                            Properties.Settings.Default.ConformanceSettings = writer.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        Properties.Settings.Default.ConformanceSettings = "";
                    }
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Re-Run all conformance test.
        /// </summary>
        private void ReRunAllTestsMnu_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Re-Run selected conformance test.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReRunSelectedTestsMnu_Click(object sender, EventArgs e)
        {

        }

        private void ReRunFailedTestsMnu_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Delete selected conformance tests.
        /// </summary>
        private void ConformanceDelete_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult ret = MessageBox.Show(this, Properties.Resources.ConformanceTestRemove, Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (ret == DialogResult.Yes)
                {
                    while (ConformanceHistoryTests.SelectedItems.Count != 0)
                    {
                        ListViewItem li = ConformanceHistoryTests.SelectedItems[0];
                        GXConformanceTest ct = li.Tag as GXConformanceTest;
                        Directory.Delete(ct.Results, true);
                        ConformanceHistoryTests.Items.Remove(li);
                    }
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Show Visualized values.
        /// </summary>
        private void showVisualizedValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectedListViewItemCollection target;
                if (IsConformanceTestSelected(sender))
                {
                    target = ConformanceTests.SelectedItems;
                }
                else
                {
                    target = ConformanceHistoryTests.SelectedItems;
                }
                if (target.Count == 1)
                {
                    ListViewItem li = target[0];
                    string path = Path.Combine((li.Tag as GXConformanceTest).Results, "Values.xml");
                    if (File.Exists(path))
                    {
                        GXValuesDlg dlg = new GXValuesDlg(Views, path);
                        dlg.Show(this);
                    }
                    else
                    {
                        throw new Exception(Properties.Resources.InvalidValueFile);
                    }
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        private void conformanceTestsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            MainRunMnu.Enabled = Devices.Count != 0 && (TransactionWork == null || !TransactionWork.IsRunning);
            MainShowDurationsMnu.Enabled = MainOpenContainingFolderBtn.Enabled = MainShowValuesBtn.Enabled = MainShowLogBtn.Enabled = MainShowReportBtn.Enabled = ConformanceTests.SelectedItems.Count != 0;
        }

        /// <summary>
        /// Save values to xml file.
        /// </summary>
        private void ExportMenu_Click(object sender, EventArgs e)
        {
            try
            {
                GXDLMSMeter dev = GetSelectedDevice();
                if (dev != null)
                {
                    SaveFileDialog dlg = new SaveFileDialog();
                    dlg.Filter = Properties.Resources.ValuesFilterTxt;
                    dlg.DefaultExt = ".xml";
                    dlg.FileName = dev.Name;
                    dlg.InitialDirectory = Directory.GetCurrentDirectory();
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        dev.Objects.Save(dlg.FileName, null);
                    }
                }
            }
            catch (Exception Ex)
            {
                GXLogWriter.WriteLog(Ex.ToString());
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Load values from xml file.
        /// </summary>
        private void ImportMenu_Click(object sender, EventArgs e)
        {
            string file = null;
            try
            {
                GXDLMSMeter dev = GetSelectedDevice();
                if (dev != null)
                {
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Multiselect = false;
                    dlg.InitialDirectory = Directory.GetCurrentDirectory();
                    dlg.FileName = dlg.FileName = dev.Name;
                    dlg.Filter = Properties.Resources.ValuesFilterTxt;
                    dlg.DefaultExt = ".xml";
                    dlg.ValidateNames = true;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        file = dlg.FileName;
                        if (File.Exists(file))
                        {
                            GXDLMSObjectCollection.Load(file, dev.Objects);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                if (file != null)
                {
                    mruManager.Remove(file);
                }
                GXLogWriter.WriteLog(Ex.ToString());
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Update descriptions.
        /// </summary>
        private void UpdateDescriptionsMenu_Click(object sender, EventArgs e)
        {
            try
            {
                //Device list is selected.
                foreach (GXDLMSDevice dev in Devices)
                {
                    GXDLMSConverter c = new GXDLMSConverter(dev.Standard);
                    foreach (GXDLMSObject it in dev.Objects)
                    {
                        it.Description = c.GetDescription(it.LogicalName, it.ObjectType)[0];
                        ListViewItem item = ObjectListItems[it] as ListViewItem;
                        if (item != null)
                        {
                            item.Text = it.LogicalName + " " + it.Description;
                        }
                        TreeNode t = ObjectTreeItems[it] as TreeNode;
                        if (t != null)
                        {
                            t.Text = it.LogicalName + " " + it.Description;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Read all attributes of selected object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadObjectMnu_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                //Set focus to the tree or Read count do not updated.
                this.ObjectTree.Focus();
                if (this.ObjectTree.SelectedNode != null)
                {
                    TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Read, OnError, null, new object[] { this.ObjectTree.SelectedNode.Tag, SelectedView, true });
                    TransactionWork.Start();
                }
            }
            else
            {
                if (this.ObjectList.SelectedItems.Count != 0)
                {
                    GXDLMSObjectCollection items = new GXDLMSObjectCollection();
                    foreach (ListViewItem it in this.ObjectList.SelectedItems)
                    {
                        items.Add(it.Tag as GXDLMSObject);
                    }
                    TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Read, OnError, null, new object[] { items, SelectedView, true });
                    TransactionWork.Start();
                }
            }
        }

        /*--------------------METHODE QUI EST APPELLE LORQUE L'ON CLIQUE SUR LE ToolStripMenuItem addRoutine------------*/

        private void addRoutine_Click(object sender, EventArgs e)
        {
         /* Methode permettant d'ajouter un objet  la routine */

            int valeurRoutine =  Convert.ToInt32(((ToolStripMenuItem)sender).Tag);

            if (valeurRoutine == 0)
            {
                valeurRoutine = 1;
            }
         //   textBox1.Text = "Valeur Routine ....  " + valeurRoutine;
            if (addRoutine.Text.Equals("add from routine"))
            {

                addRoutine.Text = "romove from routine";
                addRoutine.DropDownItems.Clear();
                routineList.Add(ObjectTree.SelectedNode.Text);
              //  textBox1.Text = ObjectTree.SelectedNode.Text+ " ---> " + routineList.Count;


                if (ObjectTree.SelectedNode.Parent.Parent.Text.Equals("Devices"))
                {

                    if (!routine2.ContainsKey(ObjectTree.SelectedNode.Parent.Text))
                    {

                        Dictionary<String, int> d = new Dictionary<string, int>();
                        d.Add(ObjectTree.SelectedNode.Text, valeurRoutine);
                        routine2.Add(ObjectTree.SelectedNode.Text, d);
                        routineStatu(ObjectTree.SelectedNode);
                        SettingSerial s = new SettingSerial();
                        Dictionary<string, Dictionary<String, int>> n = routine2;
                        s.setDectionnaire(n);
                        string initDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
                        initDir = Path.Combine(initDir, "routineList.xml");
                        s.Enregistrer(initDir);
                        textBox1.Text = routine2[ObjectTree.SelectedNode.Text][ObjectTree.SelectedNode.Text] + " ";

                    }
                }
                else
                {
                    if (!routine2.ContainsKey(ObjectTree.SelectedNode.Parent.Parent.Text))
                    {


                        Dictionary<String, int> d = new Dictionary<string, int>();
                        d.Add(ObjectTree.SelectedNode.Text, valeurRoutine);
                        routine2.Add(ObjectTree.SelectedNode.Parent.Parent.Text,d);
                        routineStatu(ObjectTree.SelectedNode);
                        SettingSerial s = new SettingSerial();
                        Dictionary<string, Dictionary<String, int>> n = routine2;
                        s.setDectionnaire(n);
                        string initDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
                        initDir = Path.Combine(initDir, "routineList.xml");
                        s.Enregistrer(initDir);
                        textBox1.Text = routine2[ObjectTree.SelectedNode.Parent.Parent.Text][ObjectTree.SelectedNode.Text] + " ";
                    }
                    else
                    {

                     


                        routine2[ObjectTree.SelectedNode.Parent.Parent.Text].Add(ObjectTree.SelectedNode.Text, valeurRoutine);
                        SettingSerial s = new SettingSerial();
                        routineStatu(ObjectTree.SelectedNode);
                        Dictionary<string, Dictionary<String, int>> n = routine2;
                        s.setDectionnaire(n);
                        string initDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
                        initDir = Path.Combine(initDir, "routineList.xml");
                        s.Enregistrer(initDir);
                        textBox1.Text = routine2[ObjectTree.SelectedNode.Parent.Parent.Text][ObjectTree.SelectedNode.Text] + " ";

                    }
                }
               //textBox1.Text = ObjectTree.SelectedNode.Parent.Parent.Parent.Text;

            }
            else
            {
                


               
               
                routine2[ObjectTree.SelectedNode.Parent.Parent.Text].Remove(ObjectTree.SelectedNode.Text);
                ObjectTree.SelectedNode.ImageIndex = 7;
                ObjectTree.SelectedNode.SelectedImageIndex = 7;
                if (routine2[ObjectTree.SelectedNode.Parent.Parent.Text].Count == 0)
                {
                    routine2.Remove(ObjectTree.SelectedNode.Parent.Parent.Text);

                }

                routineList.Remove(ObjectTree.SelectedNode.Text);
                //textBox1.Text = ObjectTree.SelectedNode.Parent.Parent.Parent.Text;
                ObjectTree.SelectedNode.ImageIndex = 7;
                ObjectTree.SelectedNode.SelectedImageIndex = 7;

                SettingSerial s = new SettingSerial();
                Dictionary<string, Dictionary<String, int>> n = routine2;
                s.setDectionnaire(n);
                string initDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
                initDir = Path.Combine(initDir, "routineList.xml");
                s.Enregistrer(initDir);



            }

           // textBox1.Text += "\n  routine2.count " + routine2.Count;

        }




        private void addRoutine_DropDownClosed(object sender, EventArgs e)
        {
            textBox1.Text += "\n----->  test de fin";
        }
        private void contextMenuStrip1_VisibleChanged(object sender, EventArgs e)
        {
            /* Cette methoe est appelée lorsque le contextMenuStrips est visible */

            try
            {

                if (contextMenuStrip1.Visible)
                {
                    textBox1.Text = " routine.ContainsKey = " + routine.ContainsKey(ObjectTree.SelectedNode.Text);
                    if (routine2.ContainsKey(ObjectTree.SelectedNode.Parent.Parent.Text))
                    {
                      
       
                        textBox1.Text = "VALEUR  " + ObjectTree.SelectedNode.Text;
                        if (routine2[ObjectTree.SelectedNode.Parent.Parent.Text].Keys.Contains(ObjectTree.SelectedNode.Text))
                        {
                            textBox1.Text = " on entre dans le if if ";
                           //textBox1.Text += "\n routine.ContainsValue(li) = " + routine.ContainsValue(li);
                            addRoutine.Text = "romove from routine";
                            addRoutine.DropDownItems.Clear();
                        }
                        else
                        {
                            addRoutine.DropDownItems.Clear();
                            addRoutine.Text = "add from routine";

                            ToolStripMenuItem newChild;
                            newChild = new ToolStripMenuItem();
                            newChild.Text = "10 minutes";
                            newChild.Tag = 1;
                            newChild.Click += new EventHandler(addRoutine_Click);
                            addRoutine.DropDownItems.Add(newChild);


                            ToolStripMenuItem newChild2;
                            newChild2 = new ToolStripMenuItem();
                            newChild2.Text = "1 hour";
                            newChild2.Tag = 6;
                            newChild2.Click += new EventHandler(addRoutine_Click);
                            addRoutine.DropDownItems.Add(newChild2);


                            ToolStripMenuItem newChild3;
                            newChild3 = new ToolStripMenuItem();
                            newChild3.Text = "00h00";
                            newChild3.Tag = 24;
                            newChild3.Click += new EventHandler(addRoutine_Click);
                            addRoutine.DropDownItems.Add(newChild3);

                        }
                    }
                    else
                    {

                        //add the new child to the parent DropDownItems collection
                        addRoutine.DropDownItems.Clear();
                        addRoutine.Text = "add from routine";
                        ToolStripMenuItem newChild;
                        newChild = new ToolStripMenuItem();
                        newChild.Text = "10 minutes";
                        newChild.Tag = 1;
                        newChild.Click += new EventHandler(addRoutine_Click);
                        addRoutine.DropDownItems.Add(newChild);

                        
                        ToolStripMenuItem newChild2;
                        newChild2 = new ToolStripMenuItem();
                        newChild2.Text = "1 hour";
                        newChild2.Tag = 6;
                        newChild2.Click += new EventHandler(addRoutine_Click);
                        addRoutine.DropDownItems.Add(newChild2);

                        
                        ToolStripMenuItem newChild3;
                        newChild3 = new ToolStripMenuItem();
                        newChild3.Text = "00h00";
                        newChild3.Tag = 24;
                        newChild3.Click += new EventHandler(addRoutine_Click);
                        addRoutine.DropDownItems.Add(newChild3);


                    }
                }
                else
                {
                    // textBox1.Text = "add from routine";
                }
            }
            catch(Exception ex)
            {

            }

        }


        private void UpdateToLatestVersionMnu_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateCheckInfo info = null;
                //Check is Click once installed.
                if (!Debugger.IsAttached && ApplicationDeployment.IsNetworkDeployed)
                {
                    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                    try
                    {
                        info = ad.CheckForDetailedUpdate();
                    }
                    catch (DeploymentDownloadException dde)
                    {
                        MessageBox.Show("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                        return;
                    }
                    catch (InvalidDeploymentException ide)
                    {
                        MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                        return;
                    }
                    catch (InvalidOperationException ioe)
                    {
                        MessageBox.Show("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                        return;
                    }
                    if (info.UpdateAvailable)
                    {
                        try
                        {
                            ad.Update();
                            Application.Restart();
                        }
                        catch (DeploymentDownloadException dde)
                        {
                            MessageBox.Show("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }

        private void checkForNewUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Check if there are new app versions.
            try
            {
                bool isConnected = true;
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    Gurux.Win32.InternetConnectionState flags = Gurux.Win32.InternetConnectionState.INTERNET_CONNECTION_LAN | Gurux.Win32.InternetConnectionState.INTERNET_CONNECTION_CONFIGURED;
                    isConnected = Gurux.Win32.InternetGetConnectedState(ref flags, 0);
                }
                UpdateCheckInfo info = null;
                //Check is Click once installed.
                if (!Debugger.IsAttached && isConnected && ApplicationDeployment.IsNetworkDeployed)
                {
                    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                    try
                    {
                        info = ad.CheckForDetailedUpdate();
                    }
                    catch (DeploymentDownloadException dde)
                    {
                        MessageBox.Show(this, "The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                        return;
                    }
                    catch (InvalidDeploymentException ide)
                    {
                        MessageBox.Show(this, "Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                        return;
                    }
                    catch (InvalidOperationException ioe)
                    {
                        MessageBox.Show(this, "This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                        return;
                    }
                    if (info.UpdateAvailable)
                    {
                        UpdateToLatestVersionMnu.Visible = true;
                        MessageBox.Show(this, "New updates available.");
                        return;
                    }
                }
                MessageBox.Show(this, "No updates available.");
            }
            catch (Exception Ex)
            {
                Error.ShowError(this, Ex);
            }
        }
       
        public void ConnexionEnBoucle()
        {
           

            
            while (Thread.CurrentThread.IsAlive)
            {
                
                entre10Minutes = false;
                entreMinui = false;
                entreHeur = false;
                DateTime now = DateTime.Now;
                DateTime t = now.AddMinutes(-1 * (now.Minute % 10)).AddSeconds(-1 * now.Second);
                DateTime ancien = t.AddMinutes(10);
                TimeSpan interval = new TimeSpan();
                interval = interval.Add(TimeSpan.FromDays((double)ancien.DayOfYear - now.DayOfYear));
                interval = interval.Add(TimeSpan.FromHours((double)ancien.Hour - now.Hour));
                interval = interval.Add(TimeSpan.FromMinutes((double)ancien.Minute - now.Minute));
                interval = interval.Add(TimeSpan.FromSeconds((double)ancien.Second - now.Second));
                //  textBox1.Text = "t heurs" + interval.Hours + "\t  minutes:" + interval.Minutes + " \tseconde " + interval.Seconds;

                nombreDeSeconde = 3600 * interval.Hours + 60 * interval.Minutes + interval.Seconds;
                if (ancien.Minute == 0 && ancien.Hour == 0)
                {
                    entreMinui = true;
                    entreHeur = false;
                    entre10Minutes = false;
                    //textBox1.Text += "on entre dans le minuit\n";
                    dateLecture = DateTime.Today;
                }
                else if (ancien.Minute == 0 && !(ancien.Hour == 0))
                {
                    //textBox1.Text += "on entre dans le 1 heure\n";
                    entreMinui = false;
                    entreHeur = true;
                    entre10Minutes = false;
                }
                else if (ancien.Minute != 0)
                {
                    entreMinui = false;
                    entreHeur = false;
                    entre10Minutes = true;
                   // textBox1.Text += " on entre dans le 10 minutes\n";
                }
                second = 0;
                
                
                f1 = getHeure(nombreDeSeconde);
                c1 = getMinutes(nombreDeSeconde);
                b1 = getSeconde(nombreDeSeconde);
                d1 = 60;
                
                 timer = new System.Threading.Timer(deTatimrk, null, 0, 13);
                int rel = f1 * 3600 + c1 * 60 + b1;
               
                actifChronometre = true;

                int decalage =(int)((double)((rel * 27) / 594));
               // textBox1.Text = " nbrSeconde :" + nombreDeSeconde + " \t nombre ref " + decalage;

                //timer1.Stop();
                /*
                if (nombreDeSeconde > decalage)
                {
                    Thread.Sleep((nombreDeSeconde - decalage) * 1000);
                }
                else
                {
                   
                }
                */
                if (nombreDeSeconde > 60)
                {
                    Thread.Sleep((nombreDeSeconde - 2) * 1000);
                }
                else
                {
                    Thread.Sleep(nombreDeSeconde * 1000);
                }
               
                timer.Change(0, Timeout.Infinite);
                textBox1.Text = " on entre dans echeantier";
                
                actifChronometre = false;
                label2.Text = "";
                label3.Text = "";
                label4.Text = "";
                label5.Text = "";

               
               
                    
                    actifSauvegarde = true;
                    if (entreMinui)
                    {
                        actifSauvegarde = true;
                        LancementLectureMinui();
                    }
                    if (entreHeur)
                    {
                        actifSauvegarde = true;
                        LancementLectureHeure();
                    }
                    if (entre10Minutes)
                    {
                        actifSauvegarde = true;
                        LancementLecture10Minutes();
                    }
                    
                

                while (ConnectBtn.Checked)
                {
                   
                }
               
                
            }

        }

        
      
        private void deTatimrk(object state)
        {
           

            d1--;
            if (d1 == 0)
            {
                b1--;
                if (c1 == 0)
                {
                    d1 = 62 + (int)((double)((nombreDeSeconde * 10) / 565));
                }
                else
                {
                    d1 = 62;
                }

            }
            if (f1 == 0 && c1 == 0 && b1 == 0)
            {
                actifChronometre = false;
                label2.Text = "0";
                label3.Text = "0";
                label4.Text = "0";
                label5.Text = "0";
            }
            if (b1 == 0)
            {
                c1--;
                
                b1 = 60;
                
              

            }
            if (c1 < 0)
            {
                f1--;
                c1 = 60;
            }
            if (actifChronometre)
            {
                try
                {

                    label2.Text = f1.ToString();
                    label3.Text = c1.ToString();
                    label4.Text = b1.ToString();
                    label5.Text = d1.ToString();
                }
                catch(Exception ex)
                {

                    label2.Text = f1.ToString();
                    label3.Text = c1.ToString();
                    label4.Text = b1.ToString();
                    label5.Text = d1.ToString();
                }
            }
            
            
           

           
        }

        private void deTatimrkThread(object state)
        {

            DateTime now1 = DateTime.Now;
            if (pasConnexion)
            {
                if (ConnectBtn.Checked)
                {
                    pasConnexion = false;
                    timerThread.Change(0, Timeout.Infinite);
                }
                mili++;
                if (mili == 60)
                {
                    sec++;
                    mili = 0;
                    textBox1.Text = " " + sec;
                }
                if (sec == 36)
                {

                    if (!ConnectBtn.Checked)
                    {

                        pasConnexion = false;
                        timerThread.Change(0, Timeout.Infinite);
                        textBox1.Text = " on terminie ici";
                    }
                }
            }

        }



        public void ListerValeur()
        {
            //  Console.WriteLine(this.ObjectTree.Size);
            textBox1.Text = "on entre dans liste valeur";
            myThread = new Thread(new ThreadStart(ConnexionEnBoucle));
            myThread.Start();
            myThread.IsBackground = true;
        
        }



        private void button1_Click(object sender, EventArgs e)
        {

           
           
            if (!actifBoucle)
            {
              
               
                
                
               // ListerValeur();

                
                if (!actifThread)
                {
                    premierFois = true;
                   // initialisationVariable();
                    ListerValeur();
                   
                    actifThread = true;
                    //  timer1.Start();

                    //  button1.Text = "Stop";
                    this.button1.Image = global::GXDLMSDirector.Properties.Resources.pause;
                    actifBoucle = true;

                    
                   // actifChronometre = true;
                }
                   
                
              


            }
            else
            {
               
                
            
              
                if (ConnectBtn.Checked)
                {
                    connection();
                }

                actifThread = false;
                timer.Change(0, Timeout.Infinite);
                myThread.Abort();
                if (tread10Minutes != null)
                {
                    if (tread10Minutes.IsAlive)
                    {
                        tread10Minutes.Abort();
                    }
                }
                if (treadHeure != null)
                {
                    if (treadHeure.IsAlive)
                    {
                        treadHeure.Abort();
                    }
                }
                if (treadMinui != null)
                {
                    if (treadMinui.IsAlive)
                    {
                        treadMinui.Abort();
                    }
                }
               
                actifChronometre = false;
                label2.Text = "";
                label3.Text = "";
                label4.Text = "";
                label5.Text = "";


                //button1.Text = "Start";
                this.button1.Image = global::GXDLMSDirector.Properties.Resources.play;
                actifBoucle = false;
            }
            

            
          

        }


       

        /*----------------cette methode permet de retourner le TreeNode selectionner a partie de sa description ---------------*/
        public TreeNode getNodeAtroutineList(String tex,String compteur)
        {

            // textBox1.Text += tex + " ---> ";
            foreach (TreeNode node in ObjectTree.Nodes)
            {
                foreach (TreeNode nod in node.Nodes)
                {
                    if (nod.Text.Equals(compteur))
                    {

                        foreach (TreeNode no in nod.Nodes)
                        {

                            foreach (TreeNode n in no.Nodes)
                            {

                                if (n.Text.Equals(tex))
                                {
                                    return n;
                                    // SelectItem(n.Tag);
                                    // 
                                }
                            }

                        }
                    }

                    //textBox1.Text += "\n\n";
                }
            }

            return null;
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        public void connection()
        {
            try
            {
                if (!ConnectBtn.Checked)
                {
                    ConnectMnu_Click(null, null);
                    ConnectBtn.Checked = true;
                }
                else
                {
                    DisconnectMnu_Click(null, null);
                    ConnectBtn.Checked = false;
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }


        /*-----------------------------------------cette methode permet de lancer la lecture des objets qui sont selectionner-----------------*/
        public void methodeTest()
        {

           // textBox1.Text = " ";
            
            if (ConnectBtn.Checked)
            {
                if (tabControl1.SelectedIndex == 0)
                {
                   //textBox1.Text = "on entre ici";
                    this.ObjectTree.Focus();
                    textBox1.Text = "le test de savoir .....    " + this.ObjectTree.SelectedNode.Text;
                    TransactionWork = new GXAsyncWork(this, OnAsyncStateChange, Read, OnError, null, new object[] { this.ObjectTree.SelectedNode.Tag, SelectedView });

                   // textBox1.Text = TransactionWork.IsRunning+" ";
                    TransactionWork.Start();
                   
                    // textBox1.Text += "------>" + TransactionWork.IsCanceled + " ----->";
                    if (TransactionWork.IsRunning)
                    {
                       // textBox1.Text += "------>" + TransactionWork.IsRunning + " ----->";
                        while (TransactionWork.IsRunning)
                        {

                        }
                    }

                   
                }
            }

          

        }

        /*--------------------------------Activation des bouton radio de la courbe de charge--------------------------*/
        public void RadioActif()
        {

            foreach (Control a in ((Form)SelectedView).Controls)
            {

                foreach (Control b in a.Controls)
                {


                    if (b.Name == "groupBox2")
                    {
                        foreach (Control j in b.Controls)
                        {
                            // textBox1.Text += j.Name + "   " + j.GetType().ToString() + "\n";

                            textBox1.Text += " \n " + j.Name + "    " + j.GetType().ToString();
                            if (j as System.Windows.Forms.RadioButton != null)
                            {
                                System.Windows.Forms.RadioButton bt = j as System.Windows.Forms.RadioButton;
                                if (j.Name.Equals("ReadFromRB"))
                                {

                                    bt.Select();
                                    //  bt.Enabled = false;
                                }
                            }
                            
                            if (j as DateTimePicker != null)
                            {
                                DateTimePicker v = j as DateTimePicker;
                                v.Enabled = true;
                                
                                if (v.Name == "ToPick")
                                {
                                    v.Enabled = false;
                                //    textBox1.Text = v.Value.ToString() + "comment sa marche";
                                   
                                    v.Value = dateLecture.AddDays(1);

                                }
                                if (v.Name == "StartPick")
                                {
                                    v.Enabled = false;
                                    //    textBox1.Text = v.Value.ToString() + "comment sa marche";
                                  
                                    v.Value = dateLecture;



                                }

                            }
                        }
                    }
                }
            }

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
        public void Chronometre()
        {
           
            timer1.Start();
          
        }


       

        private void button2_Click(object sender, EventArgs e)
        {
            /*
            SettingSerial s = new SettingSerial();
            Dictionary<string, Dictionary<String, int>> n = routine2;
           s.setDectionnaire(n);
            */
            //  s.Enregistrer("test3.xml");

            //SetsettingSave(s);



            //textBox1.Text = routine2.Count + "nombre d'element ";
            /*
            foreach(String key in routine2.Keys)
            {
                textBox1.Text += " " + key + " \n";
                foreach(String key1 in routine2[key].Keys)
                {
                    textBox1.Text += " \n description : " + key1;
                }
                textBox1.Text += "\n\n";
            }
            */
            //Compteur c = new Compteur();
            //RoutineObjet ob = new RoutineObjet();
            //this.ObjectTree.SelectedNode.ImageIndex = 13;

            /*
            String Path1 = System.Windows.Forms.Application.ExecutablePath;
            Path1 = Directory.GetParent(Path1).ToString(); // Tu arrives dans le répertoire bin
            Path1 = Directory.GetParent(Path1).ToString();
            Path1 = Directory.GetParent(Path1).ToString();
            String Path11 = Path1 + "\\ResourceExterne\\routineDevice1.bmp";
            String Path12 = Path1 + "\\ResourceExterne\\routineDevice2.bmp";
            String Path13 = Path1 + "\\ResourceExterne\\routineDevice3.bmp";
            // Image newImage = Image.FromFile(@"C:\Users\MBATHE PAUL\Desktop\projet Gurux final\GXDLMSDirector-master\Development\Resources\routineDevice.bmp");
            System.Drawing.Image newImage1 = System.Drawing.Image.FromFile(Path11);
            System.Drawing.Image newImage2 = System.Drawing.Image.FromFile(Path12);
            System.Drawing.Image newImage3 = System.Drawing.Image.FromFile(Path13);
            imageList1.Images.Add(newImage1);
            imageList1.Images.Add(newImage2);
            imageList1.Images.Add(newImage3);
            ResXResourceWriter writer = new ResXResourceWriter("newresource.resx");
            writer.AddResource("imageList1.ImageStream", imageList1.ImageStream);
            writer.Generate();
            writer.Close();
            writer.Dispose();*/

        }

        public void chargerRoutine()
        {
            string initDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
            initDir = Path.Combine(initDir, "routineList.xml");// fichier se trouvant dans document 

            if (File.Exists(initDir))
            {
                //le fichier est chargé s'il existe
                textBox1.Text = " le fichier est chargé";
                routine2 = SettingSerial.Charger(initDir);

            }
            else
            {
                routine2 = new Dictionary<string, Dictionary<String, int>>();
            }
        }
        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            textBox1.Text = " bonjour chronometre";

            if (pasConnexion)
            {
                if (ConnectBtn.Checked)
                {
                    pasConnexion = false;
                }
                mili++;
                if (mili == 60)
                {
                    sec++;
                    mili = 0;
                    textBox1.Text = " " + sec;
                }
                if (sec == 40)
                {
                   
                    if (!ConnectBtn.Checked)
                    {
                        pasConnexion = false;
                        textBox1.Text = " on terminie ici";
                    }
                }
            }
             
            

            //   actifChronometre = true;
            if (actifChronometre)
            {
              

                d1--;
                if (d1 == 0)
                {
                    b1--;
                    d1 = 59;
                }
                if(f1==0 && c1==0 && b1 == 0)
                {
                    actifChronometre = false;
                    label2.Text = "0";
                    label3.Text = "0";
                    label4.Text = "0";
                    label5.Text = "0";
                }
                if (b1 == 0)
                {
                    c1--;
                    b1 = 59;
                }
                if (c1 < 0)
                {
                    f1--;
                    c1 = 59;
                }
                if (actifChronometre)
                {
                    label2.Text = f1.ToString();
                    label3.Text = c1.ToString();
                    label4.Text = b1.ToString();
                    label5.Text = d1.ToString();
                }
               

            }
        }

        /* -------------------------------------------------------------Lecture et sauvegarde dans la base de donnée----------------------*/
        public void ReadInsertionBd()
        {
           /* cette methode lit les données de la courbe de charge la sauvegarde dans le bd et crée un fichier excel qui les representes*/
            foreach (Control a in ((Form)SelectedView).Controls)
            {

                foreach (Control b in a.Controls)
                {

                    if (b.Name == "groupBox2")
                    {
                        foreach (Control j in b.Controls)
                        {
                         
                            if (j as DateTimePicker != null)
                            {
                                DateTimePicker v = j as DateTimePicker;
                             
                                if (v.Name == "StartPick")
                                {
                                 
                                    timeDebut = v.Value;

                                }


                            }
                        }
                    }
                }



                foreach (Control b in a.Controls)
                {


                    if (b as TabControl != null)
                    {


                        TabControl h = b as TabControl;
                        foreach (TabPage tt in h.TabPages)
                        {
                            //textBox1.Text += tt.Name + "\n";
                            if (tt.Name == "tabPage1")
                            {

                                foreach (Control k in tt.Controls)
                                {
                                    if (k as DataGridView != null)
                                    {
                                         DataGridView m = k as DataGridView;
                                        List<List<String>> test = new List<List<String>>();
                                        List<String> jour = new List<String>();

                                        List<List<String>> insertList = new List<List<String>>();
                                        List<String> insertNom = new List<String>();


                                        jour.Add(m.Columns[4].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        jour.Add(m.Columns[5].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        jour.Add(m.Columns[6].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        jour.Add(m.Columns[7].HeaderText.Substring(36).Replace("Time integral 5", ""));

                                        insertNom.Add("Date");
                                        insertNom.Add(m.Columns[4].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        insertNom.Add(m.Columns[5].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        insertNom.Add(m.Columns[6].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        insertNom.Add(m.Columns[7].HeaderText.Substring(36).Replace("Time integral 5", ""));

                                        test.Add(jour);
                                        insertList.Add(insertNom);
                                      //  BDUtils.CreationTableProfile(jour);
                                        foreach (DataGridViewRow x in m.Rows)
                                        {
                                            List<String> jour1 = new List<String>();

                                            jour1.Add(x.Cells[4].Value.ToString());
                                            jour1.Add(x.Cells[5].Value.ToString());
                                            jour1.Add(x.Cells[6].Value.ToString());
                                            jour1.Add(x.Cells[7].Value.ToString());
                                            test.Add(jour1);

                                        }

                                        textBox1.Text = " nombre d'element " + test.Count;
                                        for (int i = 0; i < test[0].Count; i++)
                                        {

                                            textBox1.Text += "\n " + test[0][i];
                                        }
                                        //textBox1.Text = " test.count  " + test.Count;

                                        for (int i = 1; i < test.Count; i++)
                                        {
                                            List<String> insert = new List<String>();
                                            insert.Add(timeDebut.AddMinutes(i * 10).ToString());
                                            for (int j = 0; j < test[i].Count; j++)
                                            {
                                                insert.Add(test[i][j]);
                                            }
                                            insertList.Add(insert);
                                            //  BDUtils.insertionProfile(test[i], dateLecture.AddMinutes((i-1) * 10));
                                        }
                                       
                                        String chemin = System.IO.Path.Combine(FichierExcel.CreationDossier(dateLecture.ToShortDateString().Replace("/", "-")), ObjectTree.SelectedNode.Parent.Parent.Text + ".xlsx");

                                       
                                        textBox1.Text = " chemin   " + chemin;
                                       // textBox1.Text = " nom du fichier:  " + FichierExcel.creationFichiers(insertList, chemin);
                                         FichierExcel.creationFichiers(insertList,chemin);
                                        //  textBox1.Text = "creation du fichier ";
                                        /*
                                          for (int i = 0; i < test.Count; i++)
                                          {
                                              BDUtils.insertionProfile(test[i], timeDebut.AddMinutes(i * 10));
                                          }

                                          */


                                    }
                                }
                            }
                        }
                    }
                }

            }

        }
        
        private void timer2_Tick(object sender, EventArgs e)
        {

        }
       
        /*-------------------------------------------------------Lancement du thread de lecture de tous les minui-----------------*/
        public void LectureMinui()
        {
            textBox1.Text = "on execute lecture minuit";
            
            foreach (TreeNode node in ObjectTree.Nodes)
            {
                foreach (TreeNode nod in node.Nodes)
                {

                    
                    

                    if (routine2.ContainsKey(nod.Text))
                    {



                       
                        bool contient = false;
                        foreach (String text in routine2[nod.Text].Keys)
                        {
                            if (routine2[nod.Text][text] == 24)
                            {
                                contient = true;
                            }
                        }


                        if (contient)
                        {

                            while (ConnectBtn.Checked)
                            {

                            }

                            ObjectTree.SelectedNode = nod;
                            messeageErreur = false;
                            textBox1.Text = "connexion au compeut.... " + nod.Text + "\n";
                            ConnectMnu_Click(null, null);
                            mili = 0; sec = 0;
                            pasConnexion = true;
                            timerThread = new System.Threading.Timer(deTatimrkThread, null, 0, 10);
                            if (!ConnectBtn.Checked)
                            {

                                while (pasConnexion && !ConnectBtn.Checked)
                                {

                                }
                            }

                            if (ConnectBtn.Checked)
                            {
                                foreach (String text in routine2[nod.Text].Keys)
                                {

                                    if (routine2[nod.Text][text] == 24)
                                    {
                                        TreeNode nodeCh = getNodeAtroutineList(text, nod.Text);
                                        TreeViewEventArgs en = new TreeViewEventArgs(nodeCh);
                                        if (nodeCh != null)
                                        {
                                            this.ObjectTree.SelectedNode = nodeCh;
                                            ObjectTree_AfterSelect(null, en);
                                            RadioActif();
                                            methodeTest();
                                            SelectItem(nodeCh.Tag);
                                        }
                                    }


                                }

                                DisconnectMnu_Click(null, null);
                                Thread.Sleep(2500);
                            }
                            
                        }

                    }


                   }
                   
                }

            
            textBox1.Text = " on attend.....";
            actifSauvegarde = false;
            messeageErreur = true;
           
        }

        /*--------------------------------------Lancement du thread de lecture de tous les 1 heures------------------*/
        public void LectureHeure()
        {
            textBox1.Text = "on execute lecture 1h";
            
            foreach (TreeNode node in ObjectTree.Nodes)
            {
                foreach (TreeNode nod in node.Nodes)
                {


                    if (routine2.Count != 0)
                    {


                        textBox1.Text = " on entre pas";
                        if (routine2.ContainsKey(nod.Text))
                        {

                           
                            textBox1.Text = " on entre ici";

                            bool contient = false;
                            foreach (String text in routine2[nod.Text].Keys)
                            {
                                if (routine2[nod.Text][text] == 6)
                                {
                                    contient = true;
                                }
                            }


                            if (contient)
                            {
                                while (ConnectBtn.Checked)
                                {

                                }

                                ObjectTree.SelectedNode = nod;
                                messeageErreur = false;
                                textBox1.Text = "connexion au compeut.... " + nod.Text + "\n";
                                ConnectMnu_Click(null, null);
                                mili = 0; sec = 0;
                                pasConnexion = true;
                                timerThread = new System.Threading.Timer(deTatimrkThread, null, 0, 10);
                                if (!ConnectBtn.Checked)
                                {

                                    while (pasConnexion && !ConnectBtn.Checked)
                                    {

                                    }
                                }

                                if (ConnectBtn.Checked)
                                {

                                    foreach (String text in routine2[nod.Text].Keys)
                                    {


                                        if (routine2[nod.Text][text] == 6)
                                        {

                                            TreeNode nodeCh = getNodeAtroutineList(text, nod.Text);
                                            TreeViewEventArgs en = new TreeViewEventArgs(nodeCh);
                                            if (nodeCh != null)
                                            {
                                                this.ObjectTree.SelectedNode = nodeCh;
                                                ObjectTree_AfterSelect(null, en);
                                                RadioActif();
                                                methodeTest();
                                                SelectItem(nodeCh.Tag);
                                            }

                                        }



                                    }

                                    DisconnectMnu_Click(null, null);
                                    Thread.Sleep(2500);
                                }
                                  
                            }
                           
                        }

                    }
                }

            }

           
            textBox1.Text = " lecture heure .....";
            Thread.Sleep(2000);

            messeageErreur = true;
            actifSauvegarde = false;
            
        }


        /*----------------------------------lancement du thread de lecture des 10 minutes----------------------*/
        public void Lecture10Minute()
        {
            textBox1.Text = "on execute lecture 10";            
            foreach (TreeNode node in ObjectTree.Nodes)
            {
                foreach (TreeNode nod in node.Nodes)
                {

                    


                    if (routine2.ContainsKey(nod.Text))
                    {

                                textBox1.Text = "  " + routine2[nod.Text].Count;
                                bool contient = false;
                                foreach (String text in routine2[nod.Text].Keys)
                                {
                                    if (routine2[nod.Text][text] == 1)
                                    {
                                        contient = true;
                                    }
                                }

                                if (contient)
                                {
                                    while (ConnectBtn.Checked)
                                    {

                                    }

                                    ObjectTree.SelectedNode = nod;
                                    messeageErreur = false;
                                     textBox1.Text = "connexion au compeut.... " + nod.Text + "\n";
                                     ConnectMnu_Click(null, null);
                                    mili = 0; sec = 0;
                                    pasConnexion = true;
                                    timerThread = new System.Threading.Timer(deTatimrkThread, null, 0, 10);
                                    if (!ConnectBtn.Checked)
                                    {
                                       
                                        while (pasConnexion&&!ConnectBtn.Checked)
                                        {

                                        }
                                    }




                                     if (ConnectBtn.Checked)
                                     {
                                        foreach (String text in routine2[nod.Text].Keys)
                                        {

                                            if (!diagnosticList.Contains(getLogicalName(text)))
                                            {

                                                if (routine2[nod.Text][text] == 1)
                                                {
                                                    TreeNode nodeCh = getNodeAtroutineList(text, nod.Text);
                                                    TreeViewEventArgs en = new TreeViewEventArgs(nodeCh);
                                                    if (nodeCh != null)
                                                    {
                                                        this.ObjectTree.SelectedNode = nodeCh;
                                                        ObjectTree_AfterSelect(null, en);
                                                        RadioActif();
                                                        methodeTest();
                                                        SelectItem(nodeCh.Tag);
                                                    }
                                                }
                                            }

                                        }

                                        if (variationModeEtat)
                                        {
                                            foreach (String text in routine2[nod.Text].Keys)
                                            {

                                                if (diagnosticList.Contains(getLogicalName(text)))
                                                {

                                                    if (routine2[nod.Text][text] == 1)
                                                    {
                                                        TreeNode nodeCh = getNodeAtroutineList(text, nod.Text);
                                                        TreeViewEventArgs en = new TreeViewEventArgs(nodeCh);
                                                        if (nodeCh != null)
                                                        {
                                                            this.ObjectTree.SelectedNode = nodeCh;
                                                            ObjectTree_AfterSelect(null, en);
                                                            RadioActif();
                                                            methodeTest();
                                                            SelectItem(nodeCh.Tag);
                                                        }
                                                    }
                                                }

                                            }
                                            variationModeEtat = false;
                                        }

                                    }



                                    if (ConnectBtn.Checked)
                                    {
                                        DisconnectMnu_Click(null, null);
                                    }

                                    //  textBox1.Text = "deconexion......";
                                    Thread.Sleep(2500);
                                
                                }

                    }


                }

            }
                   
            textBox1.Text = " lecture 10 minutes.....";
            Thread.Sleep(2000);


            messeageErreur = true;
            actifSauvegarde = false;
          

        }


        public void LancementLectureMinui()
        {
            treadMinui = new Thread(new ThreadStart(LectureMinui));
            treadMinui.Start();
            treadMinui.IsBackground = true;
          treadMinui.Join();

          
        }


        public void LancementLectureHeure()
        {
            treadHeure = new Thread(new ThreadStart(LectureHeure));
            treadHeure.Start();
            treadHeure.IsBackground = true;
          treadHeure.Join();
          
        }

        public void LancementLecture10Minutes()
        {
            tread10Minutes = new Thread(new ThreadStart(Lecture10Minute));
            tread10Minutes.Start();
            tread10Minutes.IsBackground = true;
            tread10Minutes.Join();
          
        }


        public int getHeure(int seconde)
        {
            return seconde / 3600;
            
        }
        public int getMinutes(int seconde)
        {
            int tempe = seconde % 3600;
            return tempe / 60;
        }

        public int getSeconde(int seconde)
        {
            
            return seconde - getHeure(seconde) * 3600 - getMinutes(seconde) * 60;
            

        }

        /*---------------------------------------Chager le ficher des compteur au lancement de l'application--------------------*/
        public void ChargerFichier(object sender, EventArgs e)
        {
            // Cette methode permet de charge le fichier de configuration au lancement de l'application.
            string initDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
            initDir = Path.Combine(initDir, "devices.gxc");// fichier se trouvant dans document 

            if (File.Exists(initDir))
            {
                //le fichier est chargé s'il existe
                textBox1.Text = " le fichier est chargé";
                LoadFile(initDir);

            }

            else
            {
                //si le fichier n'existe pas il est crée puis chargé
                try
                {
                    String chaine = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
                    String chaine2 = "<ArrayOfGXDLMSDevice xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"Gurux1\" />";

                    StreamWriter sw = new StreamWriter(initDir, true, Encoding.UTF8);

                    sw.Write(chaine);
                    sw.WriteLine();
                    sw.Write(chaine2);
                    sw.Close();
                    textBox1.Text = "ecriture du fichier";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
                finally
                {
                    Console.WriteLine("Executing finally block.");
                }
                textBox1.Text = " le fichier n'existe pas";
                LoadFile(initDir);
            }
            chargerRoutine();
            MiseAjourRoutine();
            string initDir1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
            initDir = Path.Combine(initDir, "settingStart.xml");
            SettingStart s = (SettingStart)SettingStart.Charger("settingStart.xml");
            routineOnStartupToolStripMenuItem.Checked = s.routineStart;
            startOnStartupToolStripMenuItem.Checked = s.startWithWindows;
            if (routineOnStartupToolStripMenuItem.Checked)
            {
                button1_Click(null, null);
            }
        }

        /*---------Cette methode permet de sauvegarde les profile de charge dans un fichier excel a la demande-----------------*/
       

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }



        public void RecuperationData()
        {


            if (this.ObjectTree.SelectedNode.Parent.Text.Equals("Data"))
            {
                String description = "";
                String LogicalName = "";
                String value = "";

                foreach (Control a in ((Form)SelectedView).Controls)
                {

                    //textBox1.Text += " \n\n\n"+a.Text + "   "+a.GetType().ToString() ;

                    foreach (Control b in a.Controls)
                    {
                        textBox1.Text += " \n" + b.Name + "   " + b.GetType().ToString();

                        if (b.Name.Equals("DescriptionTB"))
                        {
                            System.Windows.Forms.TextBox d = b as System.Windows.Forms.TextBox;
                            if (d != null)
                            {
                                description = d.Text;
                            }

                        }

                        if (b.Name.Equals("ValueTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                Gurux.DLMS.UI.GXValueField c = b as Gurux.DLMS.UI.GXValueField;
                                //textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                value = c.Value.ToString();
                            }
                        }


                        if (b.Name.Equals("LogicalNameTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                Gurux.DLMS.UI.GXValueField c = b as Gurux.DLMS.UI.GXValueField;
                                // textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                LogicalName = c.Value.ToString();
                            }
                        }




                    }
                }

                textBox1.Text = "description   :" + description + "\nlogicalName  :" + LogicalName + "\n values :" + value;

            }

        }






        public void RecuperationRegister()
        {

            if (this.ObjectTree.SelectedNode.Parent.Text.Equals("Register"))
            {
                String description = "";
                String LogicalName = "";
                String value = "";
                String scaler = "";
                string unit = "";


                foreach (Control a in ((Form)SelectedView).Controls)
                {

                    //textBox1.Text += " \n\n\n"+a.Text + "   "+a.GetType().ToString() ;

                    foreach (Control b in a.Controls)
                    {
                        textBox1.Text += " \n" + b.Name + "   " + b.GetType().ToString();


                        if (b.Name.Equals("ScalerTB"))
                        {
                            System.Windows.Forms.TextBox d = b as System.Windows.Forms.TextBox;
                            if (d != null)
                            {
                                scaler = d.Text;
                            }

                        }

                        if (b.Name.Equals("DescriptionTB"))
                        {
                            System.Windows.Forms.TextBox d = b as System.Windows.Forms.TextBox;
                            if (d != null)
                            {
                                description = d.Text;
                            }

                        }

                        if (b.Name.Equals("ValueTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                Gurux.DLMS.UI.GXValueField c = b as Gurux.DLMS.UI.GXValueField;
                                //textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                value = c.Value.ToString();
                            }
                        }


                        if (b.Name.Equals("LogicalNameTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                Gurux.DLMS.UI.GXValueField c = b as Gurux.DLMS.UI.GXValueField;
                                // textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                LogicalName = c.Value.ToString();
                            }
                        }


                        if (b.Name.Equals("UnitTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                System.Windows.Forms.ComboBox c = b as System.Windows.Forms.ComboBox;
                                // textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                c.Enabled = true;
                                unit = c.SelectedValue.ToString();

                                c.Enabled = false;
                            }
                        }

                    }
                }
                if (getLogicalName(this.ObjectTree.SelectedNode.Text).Equals("0.0.97.97.2.255"))
                {
                    if (!value.Equals(valeurModeEtatCourant))
                    {
                        variationModeEtat = true;
                        valeurModeEtatCourant = value;
                       // textBox1.Text = "description   :" + description + "\nlogicalName  :" + LogicalName + "\n values :" + value + "\nscaler :" + scaler + "\nunit:" + unit;

                    }
                }
                else
                {
                    textBox1.Text = "description   :" + description + "\nlogicalName  :" + LogicalName + "\n values :" + value + "\nscaler :" + scaler + "\nunit:" + unit;

                }

            }

        }

        public void RecuperationRegisterMonitor()
        {

            if (this.ObjectTree.SelectedNode.Parent.Text.Equals("RegisterMonitor"))
            {
                String description = "";
                String LogicalName = "";
                String value = "";
                foreach (Control a in ((Form)SelectedView).Controls)
                {
                    textBox1.Text = a.Controls.Count + "";

                    foreach (Control b in a.Controls)
                    {
                        //textBox1.Text+=" \n"+b.GetType().ToString();


                        if (b.Name.Equals("ActionsLV"))
                        {
                            System.Windows.Forms.ListView c = b as System.Windows.Forms.ListView;
                            textBox1.Text = c.Columns.Count + " ";
                            foreach (ListViewItem l in c.Items)
                            {

                                value = l.Text;
                            }

                        }
                        if (b.Name.Equals("DescriptionTB"))
                        {

                            description = (b as System.Windows.Forms.TextBox).Text;
                        }

                        if (b.Name.Equals("LogicalNameTB"))
                        {
                            LogicalName = (b as Gurux.DLMS.UI.GXValueField).Value.ToString();
                        }
                    }
                }


                textBox1.Text = "description   :" + description + "\nlogicalName  :" + LogicalName + "\n values :" + value;
            }

        }

        public void RecuperationExtendedRegister()

        {

            if (this.ObjectTree.SelectedNode.Parent.Text.Equals("ExtendedRegister"))
            {

                String LogicalName = "";
                String value = "";
                String scaler = "";
                String unit = "";
                String statu = "";
                String captureTime = "";

                foreach (Control a in ((Form)SelectedView).Controls)
                {

                    //textBox1.Text += " \n\n\n"+a.Text + "   "+a.GetType().ToString() ;

                    foreach (Control b in a.Controls)
                    {
                        textBox1.Text += " \n" + b.Name + "   " + b.GetType().ToString();


                        if (b.Name.Equals("ScalerTB"))
                        {
                            System.Windows.Forms.TextBox d = b as System.Windows.Forms.TextBox;
                            if (d != null)
                            {
                                scaler = d.Text;
                            }

                        }



                        if (b.Name.Equals("ValueTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                Gurux.DLMS.UI.GXValueField c = b as Gurux.DLMS.UI.GXValueField;
                                //textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                value = c.Value.ToString();
                            }
                        }

                        if (b.Name.Equals("StatusTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                Gurux.DLMS.UI.GXValueField c = b as Gurux.DLMS.UI.GXValueField;
                                //textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                statu = c.Value.ToString();
                            }
                        }

                        if (b.Name.Equals("CaptureTimeTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                Gurux.DLMS.UI.GXValueField c = b as Gurux.DLMS.UI.GXValueField;
                                //textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                captureTime = c.Value.ToString();
                            }
                        }


                        if (b.Name.Equals("LogicalNameTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                Gurux.DLMS.UI.GXValueField c = b as Gurux.DLMS.UI.GXValueField;
                                // textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                LogicalName = c.Value.ToString();
                            }
                        }


                        if (b.Name.Equals("UnitTB"))
                        {
                            if (b as Gurux.DLMS.UI.GXValueField != null)
                            {
                                System.Windows.Forms.ComboBox c = b as System.Windows.Forms.ComboBox;
                                // textBox1.Text += " \n" + c.Value.ToString() + "   " + c.ToString();
                                c.Enabled = true;
                                unit = c.SelectedValue.ToString();

                                c.Enabled = false;
                            }
                        }

                    }
                }

                textBox1.Text = "captureTieme   :" + captureTime + "\nlogicalName  :" + LogicalName + "\n values :" + value + "\nscaler :" + scaler + "\nunit:" + unit + "\n statu" + statu;


            }

        }
        /*
        public static async void periodicTask2(object p)
        {
            CancellationToken cancellationToken = (CancellationToken)p;
            DateTime date = DateTime.Now;
            DateTime startTime = DateTime.Now;
            int count = 0;
            int next;
            while (!cancellationToken.IsCancellationRequested)
            {
                date = date.AddMilliseconds(PERIOD);
                TimeSpan delta = DateTime.Now - startTime.AddMilliseconds(count * PERIOD);

                taskBody("1", DateTime.Now, Convert.ToInt32(delta.TotalMilliseconds));

                count++;
                next = Convert.ToInt32((date - DateTime.Now).TotalMilliseconds);
                if (next >= 0)
                {
                    await Task.Delay(next, cancellationToken);
                }
                else
                {
                    raiseOccurenceDelayed("2");
                }
            }
        }
        */
        public String getLogicalName(String desc)
        {

            return desc.Remove(desc.IndexOf(" "), desc.Length - desc.IndexOf(" "));
        }
        
        public void SetsettingSave(SettingSerial s)
        {
            //On crée une instance de XmlSerializer dans lequel on lui spécifie le type
            XmlSerializer serializer = new XmlSerializer(typeof(SettingSerial));
            StreamWriter ecrivain = new StreamWriter("D:\\test.xml");
            serializer.Serialize(ecrivain, s);
            ecrivain.Close();
        }

        public void routineStatu(TreeNode node)
        {


            try
            {

          
                   
                    if (routine2.ContainsKey(node.Parent.Parent.Text))
                    {


                       
                        if (routine2[node.Parent.Parent.Text].Keys.Contains(node.Text))
                        {
                           
                         
                            
                             
                            int valeur = routine2[node.Parent.Parent.Text][node.Text];
                            if (valeur == 1)
                            {
                                node.ImageIndex = 13;
                                node.SelectedImageIndex = 13;
                            }
                            else if (valeur == 6)
                            {
                                node.ImageIndex = 14;
                                node.SelectedImageIndex = 14;
                            }
                            else {
                                node.ImageIndex = 15;
                                node.SelectedImageIndex = 15;
                            }

                            }
                        else
                        {
                           
                          

                        }
                    }
                    else
                    {

                        //add the new child to the parent DropDownItems collection
                      

                   

                    }

               
            }catch(Exception ex)
            {
              
            }
          //  SettingStart s = new SettingStart();
        }


        public void MiseAjourRoutine()
        {
            foreach (TreeNode node in ObjectTree.Nodes)
            {
                foreach (TreeNode nod in node.Nodes)
                {
                    

                        foreach (TreeNode no in nod.Nodes)
                        {

                            foreach (TreeNode n in no.Nodes)
                            {

                               routineStatu(n);
                                
                            }

                        }
                    

                }
            }
        }

        private void startOnStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
            if (!startOnStartupToolStripMenuItem.Checked)
            {
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // Ajout:
                rkApp.SetValue("Gurux", System.Windows.Forms.Application.ExecutablePath.ToString());

               
                SettingStart s = (SettingStart)SettingStart.Charger("settingStart.xml");
                s.startWithWindows = !startOnStartupToolStripMenuItem.Checked;
                s.Enregistrer("settingStart.xml");

            }
            else
            {
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // Ajout:
                //  rkApp.SetValue("NomClef", System.Windows.Forms.Application.ExecutablePath.ToString());

                //Suppression:
                rkApp.DeleteValue("Gurux", false);
               
               
                SettingStart s = (SettingStart)SettingStart.Charger("settingStart.xml");
                s.startWithWindows = !startOnStartupToolStripMenuItem.Checked;
                s.Enregistrer("settingStart.xml");

            }
            startOnStartupToolStripMenuItem.Checked = !startOnStartupToolStripMenuItem.Checked;
        }

        private void routineOnStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!routineOnStartupToolStripMenuItem.Checked)
            {

                SettingStart s = (SettingStart)SettingStart.Charger("settingStart.xml");
                s.routineStart = !routineOnStartupToolStripMenuItem.Checked;
                s.Enregistrer("settingStart.xml");
            }
            else
            {
              
                SettingStart s = (SettingStart)SettingStart.Charger("settingStart.xml");
                s.routineStart = !routineOnStartupToolStripMenuItem.Checked;
                s.Enregistrer("settingStart.xml");
            }
            routineOnStartupToolStripMenuItem.Checked = !routineOnStartupToolStripMenuItem.Checked;
        }


        public void ButtonRighChange(Object sender, EventArgs e)
        {
            System.Windows.Forms.GroupBox s = (System.Windows.Forms.GroupBox)(sender);
            button.Left = s.Width - 90;
        }
        public void ajouterBotton()
        {
            foreach (Control a in ((Form)SelectedView).Controls)
            {



                foreach (Control b in a.Controls)
                {

                    if (b as TabControl != null)
                    {
                        TabControl h = b as TabControl;
                        foreach (TabPage tt in h.TabPages)
                        {


                            if (tt.Name == "tabPage1")
                            {

                                foreach (Control k in tt.Controls)
                                {


                                    if (k as DataGridView != null)
                                    {
                                        DataGridView m = k as DataGridView;


                                        // textBox1.Text = " " + m.Rows.Count;
                                        button.Location = new System.Drawing.Point(702, 223);
                                        button.Name = "buttona";
                                        button.Size = new System.Drawing.Size(59, 23);
                                        button.TabIndex = 38;
                                        button.Text = "Export";
                                        button.UseVisualStyleBackColor = true;
                                        button.RightToLeft = RightToLeft.Yes;
                                        button.Width = 78;
                                        button.Height = 25;
                                        button.Enabled = false;
                                         this.button.Click += new System.EventHandler(exporterCourbeDeCharge);
                                        if (a.Controls.Contains(button))
                                        {
                                            a.Controls.Remove(button);
                                        }
                                       
                                        a.Controls.Add(button);
                                        button.Left = button.Parent.Width - 90;
                                        a.SizeChanged += new EventHandler(ButtonRighChange);
                                        if (m.Rows.Count != 0)
                                        {
                                            button.Enabled = true;
                                        }
                                    }

                                }
                            }
                        }
                    }

                }
            }
            //   
        }

        public void exporterCourbeDeCharge(Object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            // dlg.Filter = Properties.Resources.FilterTxt;
            dlg.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            dlg.DefaultExt = ".xlsx";
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                this.path = dlg.FileName;
                textBox1.Text = " " + this.path;
                Export(dlg.FileName);
            }
           
        }
        


        public void Export(String chemin1)
        {

            foreach (Control a in ((Form)SelectedView).Controls)
            {

                foreach (Control b in a.Controls)
                {

                    if (b.Name == "groupBox2")
                    {
                        foreach (Control j in b.Controls)
                        {

                            if (j as DateTimePicker != null)
                            {
                                DateTimePicker v = j as DateTimePicker;

                                if (v.Name == "StartPick")
                                {
                                    v.Enabled = true;
                                    timeDebut = v.Value;

                                }


                            }
                        }
                    }
                }



                foreach (Control b in a.Controls)
                {


                    if (b as TabControl != null)
                    {


                        TabControl h = b as TabControl;
                        foreach (TabPage tt in h.TabPages)
                        {
                            //textBox1.Text += tt.Name + "\n";
                            if (tt.Name == "tabPage1")
                            {

                                foreach (Control k in tt.Controls)
                                {
                                    if (k as DataGridView != null)
                                    {
                                        DataGridView m = k as DataGridView;
                                        List<List<String>> test = new List<List<String>>();
                                        List<String> jour = new List<String>();

                                        List<List<String>> insertList = new List<List<String>>();
                                        List<String> insertNom = new List<String>();


                                        jour.Add(m.Columns[4].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        jour.Add(m.Columns[5].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        jour.Add(m.Columns[6].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        jour.Add(m.Columns[7].HeaderText.Substring(36).Replace("Time integral 5", ""));

                                        insertNom.Add("Date");
                                        insertNom.Add(m.Columns[4].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        insertNom.Add(m.Columns[5].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        insertNom.Add(m.Columns[6].HeaderText.Substring(36).Replace("Time integral 5", ""));
                                        insertNom.Add(m.Columns[7].HeaderText.Substring(36).Replace("Time integral 5", ""));

                                        test.Add(jour);
                                        insertList.Add(insertNom);
                                        //  BDUtils.CreationTableProfile(jour);
                                        foreach (DataGridViewRow x in m.Rows)
                                        {
                                            List<String> jour1 = new List<String>();

                                            jour1.Add(x.Cells[4].Value.ToString());
                                            jour1.Add(x.Cells[5].Value.ToString());
                                            jour1.Add(x.Cells[6].Value.ToString());
                                            jour1.Add(x.Cells[7].Value.ToString());
                                            test.Add(jour1);

                                        }

                                        textBox1.Text = " nombre d'element " + test.Count;
                                        for (int i = 0; i < test[0].Count; i++)
                                        {

                                            textBox1.Text += "\n " + test[0][i];
                                        }
                                        //textBox1.Text = " test.count  " + test.Count;

                                        for (int i = 1; i < test.Count; i++)
                                        {
                                            List<String> insert = new List<String>();
                                            insert.Add(timeDebut.AddMinutes(i * 10).ToString());
                                            for (int j = 0; j < test[i].Count; j++)
                                            {
                                                insert.Add(test[i][j]);
                                            }
                                            insertList.Add(insert);
                                            //BDUtils.insertionProfile(test[i], timeDebut.AddMinutes((i - 1) * 10));
                                        }

                                        String chemin = System.IO.Path.Combine(FichierExcel.CreationDossier(timeDebut.ToShortDateString().Replace("/", "-")), ObjectTree.SelectedNode.Parent.Parent.Text + ".xlsx");


                                        textBox1.Text = " chemin   " + chemin1;
                                        // textBox1.Text = " nom du fichier:  " + FichierExcel.creationFichiers(insertList, chemin);
                                        FichierExcel.creationFichiers(insertList, chemin1);
                                        //  textBox1.Text = "creation du fichier ";


                                      
                                    }
                                }
                            }
                        }
                    }
                }

            }
           // textBox1.Text = " " + timeDebut.ToString();
        }

    }
}

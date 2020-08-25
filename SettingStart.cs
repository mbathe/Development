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
using System.IO;
using System.Xml.Serialization;
namespace GXDLMSDirector
{
    [Serializable]
    public class SettingStart
    {
        [XmlAttribute()]
        public bool routineStart
        {
            get; set;
        }
        [XmlAttribute()]
        public bool startWithWindows
        {
            get; set;
        }
        public SettingStart()
        {
        }


        public void Enregistrer(string chemin)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SettingStart));
            StreamWriter ecrivain = new StreamWriter(chemin, false);
            serializer.Serialize(ecrivain, this);
            ecrivain.Close();
        }

        public static SettingStart Charger(string chemin)
        {
            if (File.Exists(chemin))
            {

                XmlSerializer deserializer = new XmlSerializer(typeof(SettingStart));
                StreamReader lecteur = new StreamReader(chemin);
                SettingStart p = (SettingStart)deserializer.Deserialize(lecteur);
                lecteur.Close();
                return p;
            }else
            {
                SettingStart s = new SettingStart();
                s.startWithWindows = false;
                s.routineStart = false;
                s.Enregistrer(chemin);
                return s;
            }
        }
    }
}
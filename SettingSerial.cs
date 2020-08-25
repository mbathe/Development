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
using System.IO;
using System.Xml.Serialization;

namespace GXDLMSDirector
{
    [Serializable]
    public class SettingSerial : List<Compteur>
    {
        
        [XmlAttribute()]
        public List<Compteur> compteurs
        {
            get; set;
        }
        public SettingSerial()
        {
        }

        public static Dictionary<string, Dictionary<String, int>> getDictionnaire(SettingSerial s)
        {
            Dictionary<string, Dictionary<String, int>> routineSetting = new Dictionary<string, Dictionary<String, int>>();
            foreach (Compteur compteur in s)
            {
                String nom = "";
                Dictionary<String, int> d = new Dictionary<string, int>();
                foreach (RoutineObjet objet in compteur)
                {
                    d.Add(objet.description, objet.valeur);
                    nom = objet.NomCompteur;
                }
                routineSetting.Add(nom, d);
            }
            return routineSetting;
        }
        public void setDectionnaire(Dictionary<string, Dictionary<String, int>> routineSetting)
        {
            if (routineSetting.Keys.Count != 0)
            {
                foreach (String key in routineSetting.Keys)
                {
                   Compteur c = new Compteur();
                    c.nom = key;
                    
                    if (routineSetting[key].Count != 0)
                    {
                       foreach(String key1 in routineSetting[key].Keys)
                        {
                            RoutineObjet o = new RoutineObjet();
                            o.NomCompteur = key;
                            o.description = key1;
                            o.valeur = routineSetting[key][key1];

                           // c.routineObjects.Add(o);
                            c.Add(o);
                           
                        }

                    }
                    this.Add(c);
                }
            }
        }


        public void Enregistrer(string chemin)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SettingSerial));
            StreamWriter ecrivain = new StreamWriter(chemin,false);
            serializer.Serialize(ecrivain, this);
            ecrivain.Close();
        }

        public static Dictionary<string, Dictionary<String, int>> Charger(string chemin)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(SettingSerial));
            StreamReader lecteur = new StreamReader(chemin);
            SettingSerial p = (SettingSerial)deserializer.Deserialize(lecteur);
            lecteur.Close();

           return getDictionnaire(p);
        }


    }
}
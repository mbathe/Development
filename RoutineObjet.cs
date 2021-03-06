﻿//
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
using System.Xml.Serialization;
namespace GXDLMSDirector
{
    [Serializable]
    public class RoutineObjet
    {
        [XmlAttribute()]
        public string description
        {
            get;set;
        }
       [XmlAttribute()]
        public int valeur
        {
            get;set;
        }
        [XmlAttribute()]
        public String NomCompteur
        {
            get;set;
        }
    }
}
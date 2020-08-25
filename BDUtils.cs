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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;


namespace GXDLMSDirector
{
    internal class BDUtils
    {

        public static MySqlConnection GetDBConnection()
        {
            string host = "127.0.0.1";
            int port = 3306;
            string database = "test";
            string username = "root";
            string password = "fa22raptor";
           

            return BDMySQLUtils.GetDBConnection(host, port, database, username, password);
        }


        public static void insertion(String logicalName, String description, String valeur)
        {

            MySqlConnection connection = BDUtils.GetDBConnection();
            connection.Open();
            try
            {

                String sql = "insert into Compteur (logicalName,description,valeur) values('" + logicalName + "','" + description + "','" + valeur + "');";

                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                int rowCount = cmd.ExecuteNonQuery();

                Console.WriteLine("Row Count affected = " + rowCount);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                connection.Close();
                connection.Dispose();
                connection = null;
            }


            Console.Read();

        }



        public static void CreationTableProfile(List<String> colunm)
        {
            MySqlConnection connection = BDUtils.GetDBConnection();
            connection.Open();
            String req = "Create  Table  IF NOT EXISTS Profile(";
            for (int i = 0; i < colunm.Count; i++)
            {
                req += colunm[i] + " Varchar(50),";
            }
            req += "date DATETIME ); ";

            try
            {
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = req;

                int rowCount = cmd.ExecuteNonQuery();

                Console.WriteLine("Row Count affected = " + rowCount);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                connection.Close();
                connection.Dispose();
                connection = null;
            }

            Console.Read();
        }

       public static void  insertionProfile(List<String> colunm,DateTime time)
        {
            String req = "insert into profile(colonne1,colonne2,colonne3,colonne4,date) values('";

            for (int i = 0; i < colunm.Count; i++)
            {
                req += colunm[i] + "', '";
            }
            
            
            req += time.ToString("yyyy-MM-dd HH:mm:ss")+ "');";
            
            MySqlConnection connection = BDUtils.GetDBConnection();
            connection.Open();
           

            try
            {
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = req;

                int rowCount = cmd.ExecuteNonQuery();

                Console.WriteLine("Row Count affected = " + rowCount);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                connection.Close();
                connection.Dispose();
                connection = null;
            }
            
            Console.Read();
            
            
        }
    }
}
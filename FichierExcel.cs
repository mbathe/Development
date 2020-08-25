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
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;

namespace GXDLMSDirector
{
    internal class FichierExcel
    {
        public FichierExcel()
        {
           
        }


        public void creer()
        {

            Excel.Application xlApp = new Excel.Application();

            Excel.Workbook xlWorkbook = xlApp.Workbooks.Add();
            Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];
            Excel.Range xlRange = xlWorksheet.UsedRange;
            xlWorksheet.Cells[1, 1] = "bonjours";

            xlWorksheet.Cells[1, 1] = "bonjours 11";
            xlWorksheet.Cells[1, 2] = "bonjours 12";
            xlWorksheet.Cells[1, 3] = "bonjours 13";
            xlWorksheet.Cells[2, 1] = "bonjours 21";
            xlWorksheet.Cells[2, 2] = "bonjours 22";
            xlWorksheet.Cells[2, 3] = "bonjours 23";
            xlWorksheet.Cells[3, 1] = "bonjours 31";

            //xlWorksheet.Range["A1:H1"].Interior.Color = XlRgbColor.rgbBlue;
            xlWorksheet.Range["A1:H1"].Borders.Value = XlDataBarBorderType.xlDataBarBorderSolid;
            xlWorksheet.Columns["A:C"].ColumnWidth = 30;
            xlWorksheet.Columns["A:C"].Borders.Value = XlDataBarBorderType.xlDataBarBorderSolid;

            xlWorkbook.SaveAs("D:\\test.xlsx");
            xlWorkbook.Close();


            //quit and release
            xlApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorksheet);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorkbook);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
        }

        public static void creationFichiers(List<List<String>> test, string nomFichier)

        {

            Excel.Application xlApp = new Excel.Application();

            Excel.Workbook xlWorkbook = xlApp.Workbooks.Add();
            Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];
            Excel.Range xlRange = xlWorksheet.UsedRange;

            for (int i = 0; i < test.Count; i++)
            {
                for (int j = 0; j < test[i].Count; j++)
                {
                    xlWorksheet.Cells[i + 1, j + 1] = test[i][j];
                }
            }

            xlWorksheet.Columns["A:E"].ColumnWidth = 25;
            xlWorksheet.Columns["A:E"].Borders.Value = XlDataBarBorderType.xlDataBarBorderSolid;
          

            xlWorksheet.Columns["A:E"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            xlWorksheet.Columns["A:E"].VerticalAlignment = Excel.XlHAlign.xlHAlignCenter;

            xlWorksheet.Columns["A:E"].Font.Size = 12;

            xlWorksheet.Columns["A:E"].Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            
            xlWorkbook.SaveAs(nomFichier);
            xlWorkbook.Close();


            //quit and release
            xlApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorksheet);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorkbook);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
           
        }


        public static String CreationDossier(String dirPath)
        {
             dirPath =System.IO.Path.Combine("C:\\Profile\\" , dirPath);
            


            // Vérifiez si un chemin d'accès au répertoire existe ou non.
            bool exist = Directory.Exists(dirPath);

            // Si ce n'est pas le cas, créez-le
            if (!exist)
            {
                Console.WriteLine(dirPath + " does not exist.");
                Console.WriteLine("Create directory: " + dirPath);

                // Créez le répertoire.
                Directory.CreateDirectory(dirPath);
            }

            //Console.WriteLine("Directory Information " + dirPath);

            // Imprimer les infos de ce ledit répertoire
            // Temps de creation
            Console.WriteLine("Creation time: " + Directory.GetCreationTime(dirPath));

            // Dernier temps de l'écriture.
           //Console.WriteLine("Last Write Time: " + Directory.GetLastWriteTime(dirPath));

            // Répertoire parent.
            DirectoryInfo parentInfo = Directory.GetParent(dirPath);

         //   Console.WriteLine("Parent directory: " + parentInfo.FullName);

            
            Console.Read();
            return dirPath;
        }




    }
}
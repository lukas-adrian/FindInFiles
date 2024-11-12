#region FileHeader
// -----------------------------------------------------------------------
//  Versionskontrolle
// -----------------------------------------------------------------------
// 
//  Copyright (c) 1992-2024; AB_DATE
//  Alle Rechte vorbehalten.
// 
//  'VISUALPLAN' und 'PowerPlan' sind Markennamen von AB_DATE.
// 
//  'MicroStation', 'MDL', und 'MicroCSL' sind Markennamen der Bentley
//  Systems, Inc.
// 
//  Die Urheberrechte an diesem Quellcode liegen bei der Firma AB_DATE.
//  Veränderungen sowie die Weitergabe des Quelltextes und/oder Teile
//  des Quelltextes sind ohne Genehmigung der Firma AB_DATE nicht er-
//  laubt.
// 
// -----------------------------------------------------------------------
#endregion

namespace FindInFile.Classes;

/// <summary>
/// 
/// </summary>
public class SearchResult
{
   public string FilePath { get; set; }
   public int LineNumber { get; set; }
}

﻿//******************************************************************************************************
//  ArchiveTable`2.cs - Gbtc
//
//  Copyright © 2013, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  5/19/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//******************************************************************************************************

using System;
using openHistorian.FileStructure;

namespace openHistorian.Archive
{
    /// <summary>
    /// Represents a individual self-contained table. 
    /// </summary>
    /// <remarks>
    /// </remarks>
    public partial class ArchiveTable<TKey, TValue>
        : IDisposable
        where TKey : class, new()
        where TValue : class, new()
    {
        #region [ Members ]

        private readonly SubFileName m_fileName;
        private readonly TKey m_firstKey;
        private readonly TKey m_lastKey;
        private bool m_disposed;
        private readonly TransactionalFileStructure m_fileStructure;
        private Editor m_activeEditor;
        public ArchiveFile BaseFile;

        #endregion

        #region [ Constructors ]

        internal ArchiveTable(TransactionalFileStructure fileStructure, SubFileName fileName, ArchiveFile baseFile)
        {
            BaseFile = baseFile;
            m_fileName = fileName;
            m_fileStructure = fileStructure;
            m_firstKey = new TKey();
            m_lastKey = new TKey();
            using (ArchiveTableReadSnapshot<TKey, TValue> snapshot = AcquireReadSnapshot().CreateReadSnapshot())
            {
                snapshot.GetKeyRange(m_firstKey, m_lastKey);
            }
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Determines if the archive file has been disposed. 
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return m_disposed;
            }
        }

        /// <summary>
        /// The first key.  Note: Values only update on commit.
        /// </summary>
        public TKey FirstKey
        {
            get
            {
                return m_firstKey;
            }
        }

        /// <summary>
        /// The last key.  Note: Values only update on commit.
        /// </summary>
        public TKey LastKey
        {
            get
            {
                return m_lastKey;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Acquires a read snapshot of the current archive file.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Once the snapshot has been acquired, any future commits
        /// will not effect this snapshot. The snapshot has a tiny footprint
        /// and allows an unlimited number of reads that can be created.
        /// </remarks>
        public ArchiveTableSnapshotInfo<TKey, TValue> AcquireReadSnapshot()
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            return new ArchiveTableSnapshotInfo<TKey, TValue>(m_fileStructure, m_fileName);
        }

        /// <summary>
        /// Allows the user to get a read snapshot on the table.
        /// </summary>
        /// <returns></returns>
        public ArchiveTableReadSnapshot<TKey, TValue> BeginRead()
        {
            return AcquireReadSnapshot().CreateReadSnapshot();
        }

        /// <summary>
        /// Begins an edit of the current archive table.
        /// </summary>
        /// <remarks>
        /// Concurrent editing of a file is not supported. Subsequent calls will
        /// throw an exception rather than blocking. This is to encourage
        /// proper synchronization at a higher layer. 
        /// Wrap the return value of this function in a Using block so the dispose
        /// method is always called. 
        /// </remarks>
        /// <example>
        /// using (ArchiveFile.ArchiveFileEditor editor = archiveFile.BeginEdit())
        /// {
        ///     editor.AddPoint(key, value);
        ///     editor.AddPoint(key, value);
        ///     editor.Commit();
        /// }
        /// </example>
        public Editor BeginEdit()
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (m_activeEditor != null)
                throw new Exception("Only one concurrent edit is supported");
            m_activeEditor = new Editor(this);
            return m_activeEditor;
        }

        /// <summary>
        /// Closes the archive file. If there is a current transaction, 
        /// that transaction is immediately rolled back and disposed.
        /// </summary>
        public void Dispose()
        {
            if (!m_disposed)
            {
                if (m_activeEditor != null)
                    m_activeEditor.Dispose();
                m_fileStructure.Dispose();
                m_disposed = true;
            }
        }

        ///// <summary>
        ///// Closes and deletes the Archive File. Also calls dispose.
        ///// If this is a memory archive, it will release the memory space to the buffer pool.
        ///// </summary>
        //public void Delete()
        //{
        //    Dispose();
        //    if (m_fileName != string.Empty)
        //    {
        //        File.Delete(m_fileName);
        //    }
        //}
       

        #endregion
    }
}
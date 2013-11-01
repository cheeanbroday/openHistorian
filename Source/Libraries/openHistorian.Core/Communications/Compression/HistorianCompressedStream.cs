﻿//******************************************************************************************************
//  HistorianCompressedStream.cs - Gbtc
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
//  8/10/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using System;
using GSF.IO;
using openHistorian.Collections;
using openHistorian.Communications.Initialization;

namespace openHistorian.Communications.Compression
{
    public class HistorianCompressedStream
        : KeyValueStreamCompressionBase<HistorianKey, HistorianValue>
    {

        ulong prevTimestamp;
        ulong prevPointID;

        public override Guid CompressionType
        {
            get
            {
                return CreateHistorianCompressedStream.TypeGuid;
            }
        }

        public override void WriteEndOfStream(BinaryStreamBase stream)
        {
            stream.Write((byte)255);
        }

        public override void Encode(BinaryStreamBase stream, HistorianKey currentKey, HistorianValue currentValue)
        {
            if (currentKey.Timestamp == prevTimestamp
                && ((currentKey.PointID ^ prevPointID) < 64)
                && currentKey.EntryNumber == 0
                && currentValue.Value1 <= uint.MaxValue //must be a 32-bit value
                && currentValue.Value2 == 0
                && currentValue.Value3 == 0)
            {
                if (currentValue.Value1 == 0)
                {
                    stream.Write((byte)((currentKey.PointID ^ prevPointID)));
                }
                else
                {
                    stream.Write((byte)((currentKey.PointID ^ prevPointID) | 64));
                    stream.Write((uint)currentValue.Value1);
                }
                prevTimestamp = currentKey.Timestamp;
                prevPointID = currentKey.PointID;
                return;
            }

            byte code = 128;

            if (currentKey.Timestamp != prevTimestamp)
                code |= 64;

            if (currentKey.EntryNumber != 0)
                code |= 32;

            if (currentValue.Value1 > uint.MaxValue)
                code |= 16;
            else if (currentValue.Value1 > 0)
                code |= 8;

            if (currentValue.Value2 != 0)
                code |= 4;

            if (currentValue.Value3 > uint.MaxValue)
                code |= 2;
            else if (currentValue.Value3 > 0)
                code |= 1;

            stream.Write(code);

            if (currentKey.Timestamp != prevTimestamp)
                stream.Write7Bit(currentKey.Timestamp ^ prevTimestamp);

            stream.Write7Bit(currentKey.PointID ^ prevPointID);

            if (currentKey.EntryNumber != 0)
                stream.Write7Bit(currentKey.EntryNumber);

            if (currentValue.Value1 > uint.MaxValue)
                stream.Write(currentValue.Value1);
            else if (currentValue.Value1 > 0)
                stream.Write((uint)currentValue.Value1);

            if (currentValue.Value2 != 0)
                stream.Write(currentValue.Value2);

            if (currentValue.Value3 > uint.MaxValue)
                stream.Write(currentValue.Value3);
            else if (currentValue.Value3 > 0)
                stream.Write((uint)currentValue.Value3);

            prevTimestamp = currentKey.Timestamp;
            prevPointID = currentKey.PointID;
        }

        public override unsafe bool TryDecode(BinaryStreamBase stream, HistorianKey key, HistorianValue value)
        {
            byte code = stream.ReadByte();
            if (code == 255)
                return false;

            if (code < 128)
            {
                if (code < 64)
                {
                    key.Timestamp = prevTimestamp;
                    key.PointID = prevPointID ^ code;
                    key.EntryNumber = 0;
                    value.Value1 = 0;
                    value.Value2 = 0;
                    value.Value3 = 0;
                }
                else
                {
                    key.Timestamp = prevTimestamp;
                    key.PointID = prevPointID ^ code ^ 64;
                    key.EntryNumber = 0;
                    value.Value1 = stream.ReadUInt32();
                    value.Value2 = 0;
                    value.Value3 = 0;
                }
                prevTimestamp = key.Timestamp;
                prevPointID = key.PointID;
                return true;
            }

            if ((code & 64) != 0) //T is set
                key.Timestamp = prevTimestamp ^ stream.Read7BitUInt64();
            else
                key.Timestamp = prevTimestamp;

            key.PointID = prevPointID ^ stream.Read7BitUInt64();

            if ((code & 32) != 0) //E is set)
                key.EntryNumber = stream.Read7BitUInt64();
            else
                key.EntryNumber = 0;

            if ((code & 16) != 0) //V1 High is set)
                value.Value1 = stream.ReadUInt64();
            else if ((code & 8) != 0) //V1 low is set)
                value.Value1 = stream.ReadUInt32();
            else
                value.Value1 = 0;

            if ((code & 4) != 0) //V2 is set)
                value.Value2 = stream.ReadUInt64();
            else
                value.Value2 = 0;

            if ((code & 2) != 0) //V1 High is set)
                value.Value3 = stream.ReadUInt64();
            else if ((code & 1) != 0) //V1 low is set)
                value.Value3 = stream.ReadUInt32();
            else
                value.Value3 = 0;
            prevTimestamp = key.Timestamp;
            prevPointID = key.PointID;

            return true;
        }

        public override void ResetEncoder()
        {
            prevTimestamp = 0;
            prevPointID = 0;
        }
    }
}

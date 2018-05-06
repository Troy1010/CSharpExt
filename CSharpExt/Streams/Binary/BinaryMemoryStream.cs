﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noggog
{
    public class BinaryMemoryStream : IBinaryStream
    {
        internal int _pos;
        internal byte[] _data;
        public int Position { get => this._pos; set => SetPosition(value); }
        public int Length => this._data.Length;
        public int Remaining => this._data.Length - this._pos;
        public bool Complete => this._data.Length <= this._pos;

        #region IBinaryStream
        long IBinaryStream.Position { get => _pos; set => SetPosition(checked((int)value)); }
        long IBinaryStream.Length => this._data.Length;
        long IBinaryStream.Remaining => this._data.Length - this._pos;
        #endregion

        public BinaryMemoryStream(byte[] data)
        {
            this._data = data;
        }

        private void SetPosition(int pos)
        {
            if (pos < 0)
            {
                throw new ArgumentException("Cannot move position to a negative value.");
            }
            _pos = pos;
        }

        public int Read(byte[] buffer)
        {
            return Read(buffer, offset: 0, amount: buffer.Length);
        }

        public int Read(byte[] buffer, int offset, int amount)
        {
            if (amount > Remaining)
            {
                amount = Remaining;
            }
            Array.Copy(_data, _pos, buffer, offset, amount);
            _pos += amount;
            return amount;
        }

        public byte[] ReadBytes(int amount)
        {
            byte[] ret = new byte[amount];
            Array.Copy(_data, _pos, ret, 0, amount);
            _pos += amount;
            return ret;
        }

        public bool ReadBool()
        {
            return _data[_pos++] > 0;
        }

        public byte ReadUInt8()
        {
            return _data[_pos++];
        }

        public byte ReadByte()
        {
            return _data[_pos++];
        }

        public ushort ReadUInt16()
        {
            _pos += 2;
            return BitConverter.ToUInt16(this._data, _pos - 2);
        }

        public uint ReadUInt32()
        {
            _pos += 4;
            return BitConverter.ToUInt32(this._data, _pos - 4);
        }

        public ulong ReadUInt64()
        {
            _pos += 8;
            return BitConverter.ToUInt64(this._data, _pos - 8);
        }

        public sbyte ReadInt8()
        {
            return (sbyte)_data[_pos++];
        }

        public short ReadInt16()
        {
            _pos += 2;
            return BitConverter.ToInt16(this._data, _pos - 2);
        }

        public int ReadInt32()
        {
            _pos += 4;
            return BitConverter.ToInt32(this._data, _pos - 4);
        }

        public long ReadInt64()
        {
            _pos += 8;
            return BitConverter.ToInt64(this._data, _pos - 8);
        }

        public string ReadString(int amount)
        {
            _pos += amount;
            return BinaryUtility.BytesToString(_data, _pos - amount, amount);
        }

        public float ReadFloat()
        {
            _pos += 4;
            return BitConverter.ToSingle(this._data, _pos - 4);
        }

        public double ReadDouble()
        {
            _pos += 8;
            return BitConverter.ToDouble(this._data, _pos - 8);
        }

        public void Dispose()
        {
            this._data = null;
        }
    }
}

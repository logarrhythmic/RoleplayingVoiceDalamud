
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VfxEditor.Utils;

namespace VfxEditor.Parsing {
    public class ParsedFlag<T> : ParsedBase where T : Enum {
        public readonly string Name;
        private readonly int Size;
        public Func<ICommand> ExtraCommandGenerator; // can be changed later

        public T Value = ( T )( object )0;

        public ParsedFlag( string name, T defaultValue, Func<ICommand> extraCommandGenerator = null, int size = 4 ) : this( name, extraCommandGenerator, size ) {
            Value = defaultValue;
        }

        public ParsedFlag( string name, Func<ICommand> extraCommandGenerator = null, int size = 4 ) {
            Name = name;
            Size = size;
            ExtraCommandGenerator = extraCommandGenerator;
        }

        public override void Read( BinaryReader reader ) => Read( reader, 0 );

        public override void Read( BinaryReader reader, int size ) {
            var intValue = Size switch {
                4 => reader.ReadInt32(),
                2 => reader.ReadInt16(),
                1 => reader.ReadByte(),
                _ => reader.ReadByte()
            };
            Value = ( T )( object )intValue;
        }

        public override void Write( BinaryWriter writer ) {
            var intValue = Value == null ? -1 : ( int )( object )Value;
            if( Size == 4 ) writer.Write( intValue );
            else if( Size == 2 ) writer.Write( ( short )intValue );
            else writer.Write( ( byte )intValue );
        }
    }
}

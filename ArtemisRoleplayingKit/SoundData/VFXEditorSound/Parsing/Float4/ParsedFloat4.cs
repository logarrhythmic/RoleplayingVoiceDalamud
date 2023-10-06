
using System.IO;
using System.Numerics;

namespace VfxEditor.Parsing {
    public class ParsedFloat4 : ParsedBase {
        public readonly string Name;
        public Vector4 Value = new(0);

        public ParsedFloat4( string name, Vector4 defaultValue ) : this( name ) {
            Value = defaultValue;
        }

        public ParsedFloat4( string name ) {
            Name = name;
        }

        public override void Read( BinaryReader reader ) => Read( reader, 0 );

        public override void Read( BinaryReader reader, int _ ) {
            Value.X = reader.ReadSingle();
            Value.Y = reader.ReadSingle();
            Value.Z = reader.ReadSingle();
            Value.W = reader.ReadSingle();
        }

        public override void Write( BinaryWriter writer ) {
            writer.Write( Value.X );
            writer.Write( Value.Y );
            writer.Write( Value.Z );
            writer.Write( Value.W );
        }
    }
}

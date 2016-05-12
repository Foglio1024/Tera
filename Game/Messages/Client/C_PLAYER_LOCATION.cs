﻿using System;

namespace Tera.Game.Messages
{
    public class C_PLAYER_LOCATION : ParsedMessage
    {
        internal C_PLAYER_LOCATION(TeraMessageReader reader) : base(reader)
        {
            Start = reader.ReadVector3f();
            Heading = reader.ReadAngle();
            unk1 = reader.ReadInt16();
            Finish = reader.ReadVector3f();
            Ltype = reader.ReadInt32();
            Speed = reader.ReadInt16();
            unk2 = reader.ReadByte();
            TimeStamp = reader.ReadInt32();
 //           Console.WriteLine($"{Time.Ticks} {Start} {Heading} -> {Finish}, S:{Speed} ,{Ltype} {unk1} {unk2} {TimeStamp}" );
        }

        public int TimeStamp { get; set; }
        public byte unk2 { get; set; }
        public short unk1 { get; set; }
        public EntityId Entity { get; }
        public Vector3f Start { get; private set; }
        public Angle Heading { get; private set; }
        public short Speed { get; private set; }
        public Vector3f Finish { get; private set; }
        public int Ltype { get; private set; }
    }
}
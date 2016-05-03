﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StegomaticProject.StegoSystemModel.Steganography
{
    class Vertex
    {
        public Vertex(byte[] messagePairArray, params Pixel[] pixels)
        {
            //assign unique ID
            this.Id = _id;
            _id++;

            //all vertices will be set to active, no matter what
            this.Active = true;

            //assign "NumberOfSamples" amount of samples (pixels) to this vertex
            PixelsForThisVertex = new Pixel[GraphTheoryBased.SamplesVertexRatio];
            PixelsForThisVertex = pixels;
            

            this.SecretMessageArray = messagePairArray;
            this.VertexValue = CalculateVertexValue();
            CalculateTargetValues();
        }

        private static short _id = 0;
        public short Id { get; }

        public byte LowestEdgeWeight { get; set; }
        public short NumberOfEdges { get; set; }

        public bool Active;
        public byte VertexValue; //value that has to correspond to a certain part of the secret message
        
        public Pixel[] PixelsForThisVertex;
        public byte[] TargetValues;
        public byte[] SecretMessageArray; //placeholder array. This comes from somewhere else

        public void CalculateTargetValues() //need to know a couple things more
        {
            TargetValues = new byte[GraphTheoryBased.SamplesVertexRatio];

            //calculate difference
            byte d = (byte) Math.Abs(this.VertexValue - SecretMessageArray[this.Id]);

            //calculate targetvalues
            for (int i = 0; i < GraphTheoryBased.SamplesVertexRatio; i++)
            {
                TargetValues[i] = (byte)(PixelsForThisVertex[i].EmbeddedValue + d);
            }
        }
        public byte CalculateVertexValue()
        {
            byte temp = 0;
            for (int i = 0; i < GraphTheoryBased.SamplesVertexRatio; i++)
            {
                temp += PixelsForThisVertex[i].EmbeddedValue;
            }
            return (byte)(temp%GraphTheoryBased.Modulo);
        }
        public void AssignWeightToVertex(byte weight)
        {
            LowestEdgeWeight = weight;
        }
        public void AssignNumberOfEdgesToVertex(short edges)
        {
            NumberOfEdges = edges;
        }

        public override string ToString()
        {
            return this.Id + "\n" + this.VertexValue + "\n";
        }
    }
}

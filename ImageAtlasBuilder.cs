using System;
using System.IO;

namespace NoZ.Import
{
    internal class ImageAtlasBuilder
    {
        // TODO: atlas format
        // TODO: list of images by name with rect and optional border
        // TODO: data



        /// <summary>
        /// Add to an existing atlas
        /// </summary>
        /// <param name="source"></param>
        public ImageAtlasBuilder(Stream source)
        {

        }

        /// <summary>
        /// Construct a new atlas
        /// </summary>
        public ImageAtlasBuilder()
        {
        }

        public void AddImage (Stream source)
        {
            // TODO: convert the image into raw binary data
            // TODO: fit the image into the atlas
            // TODO: resize atlas if needed
        }

        private void Load (Stream source)
        {
        }


        public void ToStream (Stream target)
        {
            // TODO: write atlas to the stream
        }
    }
}

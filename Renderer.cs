using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet
{
    /// <summary>
    /// Holds all images in the game.
    /// It should provide sprites for objects.
    /// </summary>
    class Renderer
    {
        // This variable holds the images shared between all bullets. It's
        //   organized by [color_code][size_index].
        public Image[][] bulletImages;
        // This goes to the spark that flies out of the player when a
        // bullet passes by the player's hitbox.
        public Image grazeSparkImage;
        // This goes to the spark the flies out when an enemy is shot.
        public Image bullseyeSparkImage;
        // hitCircleImage is displayed when the player slows down; it's an identifier.
        public Image playerImage, hitCircleImage;
        public Image bg;
        public Image bossImage;
        public Image playerBulletImage;

        /// <summary>
        /// Loads an image and gives a replacement on failure.
        /// </summary>
        /// <param name="filename">Where the image is.</param>
        /// <returns>The loaded image on success or a 1x1 Image otherwise.</returns>
        public Image LoadImage(String filename)
        {
            Image img;
            try
            {
                img = new Image("res/" + filename);
            }
            catch (ArgumentException)
            {
                img = new Image(1, 1);
            }
            return img;
        }

        /// <summary>
        /// Makes all bullet images for the first time.
        /// All images are put into the bulletImages array.
        /// </summary>
        public void MakeBulletImages()
        {
            bulletImages = new Image[Bullet.RADII.Length][];
            for (int atSize = 0; atSize < Bullet.RADII.Length; atSize++)
            {
                bulletImages[atSize] =
                    new Image[(int)Bullet.BulletColors.EndColors];
                for (int color = 0; color < bulletImages[atSize].Length; color++)
                {
                    int radius = Bullet.RADII[atSize];
                    bulletImages[atSize][color] = LoadImage("b_" +
                        (radius + radius).ToString() + "x" + (radius + radius).ToString() +
                        Bullet.GetColorByName(color) + ".png");
                }
            }
        }
    }
}

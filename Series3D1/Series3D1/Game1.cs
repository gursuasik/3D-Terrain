using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Series3D1
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        public struct VertexPositionColorNormal//ışıklandırmak için kullanılır
        {
            public Vector3 Position;
            public Color Color;
            public Vector3 Normal;
            public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(sizeof(float) * 3 + 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
            );
        }
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        GraphicsDevice device;//grafik bağdaştırıcısı için

        Effect effect;//efekt dosyası için

        //VertexPositionColor[] vertices;//üçgenin kesişme noktaları tanımlanır
        VertexPositionColorNormal[] vertices;//köşe değişken bildirimi değiştirmek için kullanılır

        Matrix viewMatrix;//kamera konumlandırmak için kullanılır
        Matrix projectionMatrix;

        private float angle = 0f;//döndürme açısı için

        int[] indices;//üçgenlerin köşe kesişme noktaları tutulur
        private int terrainWidth = 4;//sanırsam arazinin boyutları
        private int terrainHeight = 3;
        private float[,] heightData;//z ekseni koordinat bilgileri tutulur
        VertexBuffer myVertexBuffer;//hızlandırmak için
        IndexBuffer myIndexBuffer;
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            graphics.PreferredBackBufferWidth = 500;//ekran ayarları
            graphics.PreferredBackBufferHeight = 500;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            Window.Title = "Riemer's XNA Tutorials -- Series 1";

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            // TODO: use this.Content to load your game content here
            device = graphics.GraphicsDevice;//grafik bağdaştırıcısı ayarlarını atamak için

            effect = Content.Load<Effect>("effects");//efekt dosyasını yükler

            SetUpCamera();//kamera ayarları yapılır
            Texture2D heightMap = Content.Load<Texture2D>("heightmap");//resim dosyasının yüklenmesi
            LoadHeightData(heightMap);//z ekseni yükseklik ayarları yapılır
            SetUpVertices();//üçgen özelliklerini belirler
            SetUpIndices();//üçgenlerin kesişme noktarını tutan dizinin içeriği doldurulur
            CalculateNormals();//üçgenlerin normalini hesaplar, aydınlanma yapmak için
            CopyToBuffers();//hızlandırma ayarı için
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();
            // TODO: Add your update logic here
            KeyboardState keyState = Keyboard.GetState();//klavye tuşları ile döndürme işlemi yapılır
            if (keyState.IsKeyDown(Keys.Delete))
                angle += 0.05f;//döndürme açısını arttırır
            if (keyState.IsKeyDown(Keys.PageDown))
                angle -= 0.05f;
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // TODO: Add your drawing code here
            //device.Clear(Color.Black);//ara belleği temizlemek için
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);//anladığıma göre z-buffer ekler
            
            RasterizerState rs = new RasterizerState();//emin değilim çizilecek üçgen sayısını azaltarak performansı artırır
            rs.CullMode = CullMode.None;
            //rs.FillMode = FillMode.WireFrame;//katı üçgenin içeriğini boşaltır
            rs.FillMode = FillMode.Solid;//üçgeni katı modelleme yapar
            device.RasterizerState = rs;
/*
            effect.CurrentTechnique = effect.Techniques["ColoredNoShading"];
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            Matrix worldMatrix = Matrix.Identity;
            Vector3 rotAxis = new Vector3(3 * angle, angle, 2 * angle);//dönme eksenini belirlemek için sanırsam
            rotAxis.Normalize();//CreateFromAxisAngle yöntemi düzgün çalışması için gerekli bu ekseni normalleştirir
            Matrix worldMatrix = Matrix.CreateTranslation(-20.0f / 3.0f, -10.0f / 3.0f, 0) * Matrix.CreateFromAxisAngle(rotAxis, angle);
            Matrix worldMatrix = Matrix.CreateTranslation(-20.0f / 3.0f, -10.0f / 3.0f, 0) * Matrix.CreateRotationZ(angle);
            Matrix worldMatrix = Matrix.CreateRotationY(3 * angle);//y ekseni etrafında döndürme yapan matris oluşturur
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
            effect.CurrentTechnique = effect.Techniques["Pretransformed"];//efekt tekniği belirlenir
            Matrix worldMatrix = Matrix.CreateTranslation(-terrainWidth / 2.0f, 0, terrainHeight / 2.0f);
*//*
            Matrix worldMatrix = Matrix.CreateTranslation(-terrainWidth / 2.0f, 0, terrainHeight / 2.0f) * Matrix.CreateRotationY(angle);//üçgenin orta noktası etrafında döndürülür
            effect.CurrentTechnique = effect.Techniques["ColoredNoShading"];//renkli bir 3D görüntü oluşturmak için, ama ışık, gölge bilgileri belirtilmemiş, yansıtmak için teknik olarak adlandırılır
            effect.Parameters["xView"].SetValue(viewMatrix);//matrislerin bizim tekniğe geçme ihtiyacı karşılanır
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xWorld"].SetValue(worldMatrix);//döndürme açısı matrisi atanır
*/
            Matrix worldMatrix = Matrix.CreateTranslation(-terrainWidth / 2.0f, 0, terrainHeight / 2.0f) * Matrix.CreateRotationY(angle);
            effect.CurrentTechnique = effect.Techniques["Colored"];
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            Vector3 lightDirection = new Vector3(1.0f, -1.0f, -1.0f);
            lightDirection.Normalize();
            effect.Parameters["xLightDirection"].SetValue(lightDirection);
            effect.Parameters["xAmbient"].SetValue(0.1f);
            effect.Parameters["xEnableLighting"].SetValue(true);          
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)//efekt tekniğini uygulamak için
            {
                pass.Apply();
                //device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 1, VertexPositionColor.VertexDeclaration);//üçgen çizmek için tepe noktası 0 başlangıç 1 olduğunu belirler
                //device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3, VertexPositionColor.VertexDeclaration);//üçgenin kesişme noktalarının indis numaralarını tutan diziye göre üçgeni çizer
                //device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3, VertexPositionColorNormal.VertexDeclaration);//arazinin yüksek kısımlarının gölgesini yapmak için
                device.Indices = myIndexBuffer;//hızlandırma ayarları için
                device.SetVertexBuffer(myVertexBuffer);
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, indices.Length / 3);                

            }
            base.Draw(gameTime);
        }
        
        private void SetUpVertices()//üçgenin koordinat özellikleri belirlenir
        {
            //vertices = new VertexPositionColor[terrainWidth * terrainHeight];
            vertices = new VertexPositionColorNormal[terrainWidth * terrainHeight];
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainHeight; y++)
                {
                    if (heightData[x, y] < minHeight)
                        minHeight = heightData[x, y];
                    if (heightData[x, y] > maxHeight)
                        maxHeight = heightData[x, y];
                    vertices[x + y * terrainWidth].Position = new Vector3(x, heightData[x, y], -y);
                    if (heightData[x, y] < minHeight + (maxHeight - minHeight) / 4)
                        vertices[x + y * terrainWidth].Color = Color.Blue;
                    else if (heightData[x, y] < minHeight + (maxHeight - minHeight) * 2 / 4)
                        vertices[x + y * terrainWidth].Color = Color.Green;
                    else if (heightData[x, y] < minHeight + (maxHeight - minHeight) * 3 / 4)
                        vertices[x + y * terrainWidth].Color = Color.Brown;
                    else
                        vertices[x + y * terrainWidth].Color = Color.White;
                }
            }
        }
        private void SetUpCamera()//kamera konumlandırma ayarları
        {
            //viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 50), new Vector3(0, 0, 0), new Vector3(0, 1, 0));//kamera konumunu tanımlar
            //viewMatrix = Matrix.CreateLookAt(new Vector3(0, 50, 0), new Vector3(0, 0, 0), new Vector3(0, 0, -1));//kamera konumunu tanımlar
            //viewMatrix = Matrix.CreateLookAt(new Vector3(0, -40, 100), new Vector3(0, 50, 0), new Vector3(0, 1, 0));//aydınlatma bölümündeki kamera ayarı için
            
            viewMatrix = Matrix.CreateLookAt(new Vector3(60, 80, -80), new Vector3(0, 0, 0), new Vector3(0, 1, 0));//kamera konumunu tanımlar
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 300.0f);//kamera görünümü, 45 derece, en boy oranı vb.

        }
        private void SetUpIndices()//üçgenlerin köşe kesişme noktarını tutan dizinin içeriği doldurulur
        {
            indices = new int[(terrainWidth - 1) * (terrainHeight - 1) * 6];
            int counter = 0;
            for (int y = 0; y < terrainHeight - 1; y++)
            {
                for (int x = 0; x < terrainWidth - 1; x++)
                {
                    int lowerLeft = x + y * terrainWidth;
                    int lowerRight = (x + 1) + y * terrainWidth;
                    int topLeft = x + (y + 1) * terrainWidth;
                    int topRight = (x + 1) + (y + 1) * terrainWidth;
                    indices[counter++] = topLeft;//üçgenlerin kesişim indis numaraları saat yönünde sıralanır
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;
                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }
        }
        private void LoadHeightData(Texture2D heightMap)//z ekseni yükseklik ayarları yapılır
        {
            terrainWidth = heightMap.Width;
            terrainHeight = heightMap.Height;
            Color[] heightMapColors = new Color[terrainWidth * terrainHeight];
            heightMap.GetData(heightMapColors);//görüntünün her bir pikselinin rengi diziye atanır
            heightData = new float[terrainWidth, terrainHeight];
            for (int x = 0; x < terrainWidth; x++)
                for (int y = 0; y < terrainHeight; y++)
                    heightData[x, y] = heightMapColors[x + y * terrainWidth].R / 5.0f;
        }
        private void CalculateNormals()//her bir üçgen için normal hesaplar aydınlanmayı belirtmek için
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal = new Vector3(0, 0, 0);
            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index1 = indices[i * 3];
                int index2 = indices[i * 3 + 1];
                int index3 = indices[i * 3 + 2];
                Vector3 side1 = vertices[index1].Position - vertices[index3].Position;
                Vector3 side2 = vertices[index1].Position - vertices[index2].Position;
                Vector3 normal = Vector3.Cross(side1, side2);
                vertices[index1].Normal += normal;
                vertices[index2].Normal += normal;
                vertices[index3].Normal += normal;
            }
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal.Normalize();
        }
        private void CopyToBuffers()//hızlandırma yöntemi için
        {
            myVertexBuffer = new VertexBuffer(device, VertexPositionColorNormal.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);//tepe noktalarını depolamak için ekran kartı belleği ayırır
            myVertexBuffer.SetData(vertices);//grafik kartındaki belleğe bizim yerel kesişme noktaları diziden verileri kopyalar

            myIndexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);//bellekte nekadar yer açılacağını belirler sanırım
            myIndexBuffer.SetData(indices);//indisler için grafik kartı üzerine kopyalar
        }
    }
}

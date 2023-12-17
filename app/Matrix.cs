using GHelper.AnimeMatrix;
using GHelper.UI;

namespace GHelper
{
    public partial class Matrix : RForm
    {

        public AniMatrixControl matrixControl = Program.settingsForm.matrixControl;

        private bool Dragging;
        private int xPos;
        private int yPos;

        private int baseX;
        private int baseY;

        private float uiScale;

        Image picture;
        MemoryStream ms = new MemoryStream();

        public Matrix()
        {
            InitializeComponent();
            InitTheme(setDPI: true);

            Shown += Matrix_Shown;
            FormClosing += Matrix_FormClosed;

            buttonPicture.Click += ButtonPicture_Click;
            buttonReset.Click += ButtonReset_Click;

            pictureMatrix.MouseUp += PictureMatrix_MouseUp;
            pictureMatrix.MouseMove += PictureMatrix_MouseMove;
            pictureMatrix.MouseDown += PictureMatrix_MouseDown;

            trackZoom.MouseUp += TrackZoom_MouseUp;
            trackZoom.ValueChanged += TrackZoom_Changed;
            trackZoom.Value = Math.Min(val1: trackZoom.Maximum, val2: AppConfig.Get(name: "matrix_zoom", empty: 100));

            trackContrast.MouseUp += TrackContrast_MouseUp; ;
            trackContrast.ValueChanged += TrackContrast_ValueChanged; ;
            trackContrast.Value = Math.Min(val1: trackContrast.Maximum, val2: AppConfig.Get(name: "matrix_contrast", empty: 100));

            VisualiseMatrix();

            comboScaling.DropDownStyle = ComboBoxStyle.DropDownList;
            comboScaling.SelectedIndex = AppConfig.Get(name: "matrix_quality", empty: 0);
            comboScaling.SelectedValueChanged += ComboScaling_SelectedValueChanged;

            comboRotation.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRotation.SelectedIndex = AppConfig.Get(name: "matrix_rotation", empty: 0);
            comboRotation.SelectedValueChanged += ComboRotation_SelectedValueChanged; ;


            uiScale = panelPicture.Width / matrixControl.device.MaxColumns / 3;
            panelPicture.Height = (int)(matrixControl.device.MaxRows * uiScale);

        }

        private void TrackContrast_ValueChanged(object? sender, EventArgs e)
        {
            VisualiseMatrix();
        }

        private void TrackContrast_MouseUp(object? sender, MouseEventArgs e)
        {
            AppConfig.Set(name: "matrix_contrast", value: trackContrast.Value);
            SetMatrixPicture();
        }

        private void ComboRotation_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "matrix_rotation", value: comboRotation.SelectedIndex);
            SetMatrixPicture(visualise: false);
        }

        private void ComboScaling_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "matrix_quality", value: comboScaling.SelectedIndex);
            SetMatrixPicture(visualise: false);
        }

        private void Matrix_FormClosed(object? sender, FormClosingEventArgs e)
        {
            if (picture is not null) picture.Dispose();
            if (ms is not null) ms.Dispose();

            pictureMatrix.Dispose();

            GC.Collect(generation: GC.MaxGeneration, mode: GCCollectionMode.Forced);
        }

        private void VisualiseMatrix()
        {
            labelZoom.Text = trackZoom.Value + "%";
            labelContrast.Text = trackContrast.Value + "%";
        }

        private void ButtonReset_Click(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "matrix_contrast", value: 100);
            AppConfig.Set(name: "matrix_zoom", value: 100);
            AppConfig.Set(name: "matrix_x", value: 0);
            AppConfig.Set(name: "matrix_y", value: 0);

            trackZoom.Value = 100;
            trackContrast.Value = 100;

            SetMatrixPicture();

        }

        private void TrackZoom_MouseUp(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "matrix_zoom", value: trackZoom.Value);
            SetMatrixPicture();
        }

        private void TrackZoom_Changed(object? sender, EventArgs e)
        {
            VisualiseMatrix();
        }


        private void PictureMatrix_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Dragging = true;
                xPos = e.X;
                yPos = e.Y;
            }
        }

        private void PictureMatrix_MouseMove(object? sender, MouseEventArgs e)
        {
            Control c = sender as Control;
            if (Dragging && c != null)
            {
                c.Top = e.Y + c.Top - yPos;
                c.Left = e.X + c.Left - xPos;
            }
        }

        private void PictureMatrix_MouseUp(object? sender, MouseEventArgs e)
        {

            Dragging = false;

            Control c = sender as Control;

            int matrixX = (int)((baseX - c.Left) / uiScale);
            int matrixY = (int)((baseY - c.Top) / uiScale);

            AppConfig.Set(name: "matrix_x", value: matrixX);
            AppConfig.Set(name: "matrix_y", value: matrixY);

            SetMatrixPicture(visualise: false);
        }

        private void Matrix_Shown(object? sender, EventArgs e)
        {
            FormPosition();
            SetMatrixPicture();
        }

        private void SetMatrixPicture(bool visualise = true)
        {
            matrixControl.SetMatrixPicture(fileName: AppConfig.GetString(name: "matrix_picture"), visualise: visualise);
        }

        private void ButtonPicture_Click(object? sender, EventArgs e)
        {
            matrixControl.OpenMatrixPicture();

        }
        public void FormPosition()
        {
            if (Height > Program.settingsForm.Height)
            {
                Top = Program.settingsForm.Top + Program.settingsForm.Height - Height;
            }
            else
            {
                Height = Program.settingsForm.Height;
                Top = Program.settingsForm.Top;
            }

            Left = Program.settingsForm.Left - Width - 5;
        }

        public void VisualiseMatrix(string fileName)
        {

            if (picture is not null) picture.Dispose();

            using (var fs = new FileStream(path: fileName, mode: FileMode.Open))
            {

                ms.SetLength(value: 0);
                fs.CopyTo(destination: ms);
                ms.Position = 0;
                fs.Close();

                picture = Image.FromStream(stream: ms);

                int width = picture.Width;
                int height = picture.Height;

                int matrixX = AppConfig.Get(name: "matrix_x", empty: 0);
                int matrixY = AppConfig.Get(name: "matrix_y", empty: 0);
                int matrixZoom = AppConfig.Get(name: "matrix_zoom", empty: 100);

                float scale = Math.Min(val1: (float)panelPicture.Width / (float)width, val2: (float)panelPicture.Height / (float)height) * matrixZoom / 100;

                pictureMatrix.Width = (int)(width * scale);
                pictureMatrix.Height = (int)(height * scale);

                baseX = panelPicture.Width - pictureMatrix.Width;
                baseY = 0;

                pictureMatrix.Left = baseX - (int)(matrixX * uiScale);
                pictureMatrix.Top = baseY - (int)(matrixY * uiScale);

                pictureMatrix.SizeMode = PictureBoxSizeMode.Zoom;
                pictureMatrix.Image = picture;


            }




        }

    }
}

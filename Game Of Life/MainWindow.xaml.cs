using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Game_Of_Life
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Timer T_ControlesBorderVisibility;
        private Timer T_GameOfLife;
        private List<Modele> Modeles { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            T_ControlesBorderVisibility = new Timer(2000);
            T_GameOfLife = new Timer(20);

            T_ControlesBorderVisibility.Elapsed += (sender, e) =>
            {
                // Hide les contrôles
                Dispatcher.Invoke(() =>
                {
                    Animation.HideAnimation(Border_Controles);
                    T_ControlesBorderVisibility.Stop();
                });
            };

            T_GameOfLife.Elapsed += GameOfLifeNextStep;
        }

        private bool LastStepFinished = true;

        private void GameOfLifeNextStep(object? sender, ElapsedEventArgs e)
        {
            // Next step

            if (LastStepFinished)
            {
                LastStepFinished = false;

                var cells = InfiniteBoard.GetAllChildren().ToDictionary(entry => entry.Key,
                                                entry => entry.Value); // Clone

                // (double, double) x et y, int : en commun
                var cellsVides = new Dictionary<(double, double), int>();
                var cellsToDelete = new List<int[]>();

                // Toute cellule vivante ayant moins de deux voisins vivants meurt
                foreach (var cell in cells)
                {
                    int nbCellVoisin = 0;
                    ActionOnNeighborhood(cell.Key,
                        (cell_voisine_co, doesExist) =>
                        {
                            if (doesExist)
                                nbCellVoisin++;
                            else
                            {
                                // cell vide
                                int nb = 0;
                                if (!cellsVides.TryGetValue((cell_voisine_co.X, cell_voisine_co.Y), out nb))
                                    cellsVides.Add((cell_voisine_co.X, cell_voisine_co.Y), 1);
                                else
                                    cellsVides[(cell_voisine_co.X, cell_voisine_co.Y)] += 1;
                            }
                        });

                    if (nbCellVoisin < 2 || nbCellVoisin > 3)
                    {
                        cellsToDelete.Add(new int[2] { Convert.ToInt32(cell.Key.X), Convert.ToInt32(cell.Key.Y) });
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    cellsToDelete.ForEach(x =>
                    {
                        InfiniteBoard.EraseCell(x[0], x[1]); ;
                    });

                    var _cell = cellsVides.ToList().FindAll(x => x.Value == 3);
                    _cell.ForEach(x =>
                    {
                        // Cellule morte ayant 3 voisins = cellule vivante
                        InfiniteBoard.PlaceCell(Convert.ToInt32(x.Key.Item1), Convert.ToInt32(x.Key.Item2));

                    });
                });

                LastStepFinished = true;
            }
        }

        /// <summary>
        /// Effectue une action sur toutes les voisines d'une cellule
        /// </summary>
        /// <param name="cell">Cellule à qui ont check les voisines</param>
        /// <param name="action">Co de la voisine et si c'est une cellule ou non</param>
        private void ActionOnNeighborhood(Point cellCo, Action<Point, bool> action)
        {
            int originY = Convert.ToInt32(cellCo.Y);
            int originX = Convert.ToInt32(cellCo.X);

            // cell du haut existe ?
            action(new Point(originX, originY - 1), InfiniteBoard.DoesAnyCellsAlreadyExistHere(originX, originY - 1));
            // cell du bas existe?
            action(new Point(originX, originY + 1), InfiniteBoard.DoesAnyCellsAlreadyExistHere(originX, originY + 1));
            // cell de gauche existe?
            action(new Point(originX - 1, originY), InfiniteBoard.DoesAnyCellsAlreadyExistHere(originX - 1, originY));
            // cell de droite existe?
            action(new Point(originX + 1, originY), InfiniteBoard.DoesAnyCellsAlreadyExistHere(originX + 1, originY));
            // cell haut gauche
            action(new Point(originX - 1, originY - 1), InfiniteBoard.DoesAnyCellsAlreadyExistHere(originX - 1, originY - 1));
            // cell haut droite
            action(new Point(originX + 1, originY - 1), InfiniteBoard.DoesAnyCellsAlreadyExistHere(originX + 1, originY - 1));
            // cell bas gauche
            action(new Point(originX - 1, originY + 1), InfiniteBoard.DoesAnyCellsAlreadyExistHere(originX - 1, originY + 1));
            // cell bas droit
            action(new Point(originX + 1, originY + 1), InfiniteBoard.DoesAnyCellsAlreadyExistHere(originX + 1, originY + 1));
        }

        private void Border_Controles_MouseLeave(object sender, MouseEventArgs e)
        {
            T_ControlesBorderVisibility.Start();
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            T_ControlesBorderVisibility.Stop();
            Border_Controles.Opacity = 1;
        }

        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            T_GameOfLife.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //InfiniteBoard.PlaceCell(0, 0);
            //GameOfLifeNextStep(this, null);
            T_GameOfLife.Stop();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            string json_modeles = new StreamReader(
                           System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("Game_Of_Life.modele.zdt")!
                       ).ReadToEnd();

            Modeles = JsonConvert.DeserializeObject<Root>(json_modeles).Modèles;
            Modeles.ForEach(x => listView_modele.Items.Add(x.Nom));
        }

        private void listView_modele_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Modele selectedModele = Modeles.FirstOrDefault(x => x.Nom.Equals(listView_modele.SelectedItem));
            if(selectedModele != default)
            {
                T_GameOfLife.Stop();
                InfiniteBoard.ClearBoard();

                for (int x = 0; x < selectedModele.Grille.GetLength(0); x++)
                {
                    for (int y = 0; y < selectedModele.Grille.GetLength(1); y++)
                    {
                        if (selectedModele.Grille[x,y] == 1)
                            InfiniteBoard.PlaceCell(x - selectedModele.Grille.GetLength(0) / 2, y - selectedModele.Grille.GetLength(0) / 2);
                    }
                }
            }
        }
    }

    public class Modele
    {
        public string Nom { get; set; }
        public int[,] Grille { get; set; }
    }

    public class Root
    {
        public List<Modele> Modèles { get; set; }
    }

}

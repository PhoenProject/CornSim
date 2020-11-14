using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Documents.DocumentStructures;

namespace CornSim
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		int seeds = 0;
		int money = 50;
		int plantedSeeds = 0;
		bool hasGrown = false;
		bool hasWon = false;
		int growthPercent = 0;
		int sellPrice = 8;
		int buyPrice = 5;
		int fieldSpace = 50;

		Random rand = new Random();
		Timer logicTimer;

		DateTime LastAttack;
		DateTime LastActivity;

		int growthInterval = 5;

		enum Items
		{
			Scarecrow = 0,
			Harvester = 1,
			Sprinkler = 2,
			Planter = 3,
			Expansion = 4,

		}

		Dictionary<Items, int> ItemStore = new Dictionary<Items, int>()
		{
			{ Items.Scarecrow, 2250 },
			{ Items.Harvester, 1300 },
			{ Items.Planter, 1750 },
			{ Items.Sprinkler, 3300 },
			{ Items.Expansion, 250 },
		};
		Dictionary<Items, bool> BoughtItems = new Dictionary<Items, bool>();

		readonly string[] Animals = new string[]
		{
			"Crows",
			"Hawks",
			"Foxes",
			"Rabbits",
			"Squirrels",
			"Racoons",
		};

		public MainWindow()
		{
			LastAttack = DateTime.Now;
			LastActivity = DateTime.Now;

			InitializeComponent();
			UpdateSeeds();
			UpdateMoney();
			UpdatePlantedSeeds();
			UpdatePercent();
			UpdateFieldSpace();
			CreateButtons();

			logicTimer = new Timer(timer => LogicCore(), null, TimeSpan.Zero, TimeSpan.FromSeconds(growthInterval));
		}

		private void LogicCore()
		{
			this.Dispatcher.Invoke(SeedLogic);
		}

		private void SeedLogic()
		{
			if (plantedSeeds == 0)
			{
				if (BoughtItems.ContainsKey(Items.Planter))
					Planter();

				return;
			}

			if (hasGrown)
			{
				if (BoughtItems.ContainsKey(Items.Harvester))
					HarvestSeeds();

				return;
			}

			int action = rand.Next(0, 50);

			if (plantedSeeds > 25 && ((BoughtItems.ContainsKey(Items.Scarecrow) && action < 2) || action < 9))
			{
				if ((DateTime.Now - LastAttack).TotalSeconds > 90)
					AttackSeeds();
			}

			else GrowSeeds(BoughtItems.ContainsKey(Items.Sprinkler) ? rand.Next(5, 10) : rand.Next(1, 5));
		}

		private void GrowSeeds(int growth)
		{
			if ((DateTime.Now - LastActivity).TotalSeconds < growthInterval)
				return;

			growthPercent = Clamp(growthPercent + growth, 0, 100);

			if (growthPercent == 100)
				hasGrown = true;

			InfoBox.Text = $"Your {IsPlural(plantedSeeds)} grew by {growth}%";
			UpdatePercent();
		}
		private void AttackSeeds()
		{
			bool hasScarecrow = BoughtItems.ContainsKey(Items.Scarecrow);

			int lostSeeds = hasScarecrow ? rand.Next(1, 2) : rand.Next(1, 5);
			int lostPercent = hasScarecrow ? rand.Next(1, 2) : rand.Next(1, 5);

			int origSeeds = plantedSeeds;
			int origPercent = growthPercent;

			plantedSeeds = Clamp(plantedSeeds - lostSeeds, 1, int.MaxValue);
			growthPercent = Clamp(growthPercent - lostPercent, 0, 100);

			lostSeeds = origSeeds - plantedSeeds;
			lostPercent = origPercent - growthPercent;

			InfoBox.Text = $"Your field was attacked by {Animals[rand.Next(0, Animals.Length - 1)]}. You lost {lostSeeds} {IsPlural(lostSeeds)}, and growth was reverted by {lostPercent}%";

			UpdatePlantedSeeds();
			UpdateSeeds();
			UpdatePercent();

			LastAttack = DateTime.Now;
		}

		private void PlantSeeds()
		{
			if (seeds == 0)
			{
				InfoBox.Text = "You need seeds in order to plant";
				return;
			}

			if (plantedSeeds > 0)
			{
				InfoBox.Text = "You have already growing seeds";
				return;
			}

			LastActivity = DateTime.Now;

			int PlantableSeeds = seeds;

			PlantableSeeds = Clamp(PlantableSeeds, 1, fieldSpace);

			plantedSeeds = PlantableSeeds;
			seeds -= PlantableSeeds;
			InfoBox.Text = $"You have planted {PlantableSeeds} {IsPlural(seeds)}";

			UpdateSeeds();
			UpdatePlantedSeeds();

			LastAttack = DateTime.Now;
		}

		private void HarvestSeeds()
		{
			if (!hasGrown)
			{
				InfoBox.Text = $"Your {IsPlural(plantedSeeds)} have not fully grown";
				return;
			}

			int moneyGained = plantedSeeds * sellPrice;

			InfoBox.Text = $"You have harvested {plantedSeeds} {IsPlural(plantedSeeds)} for ${moneyGained}";

			money += moneyGained;
			plantedSeeds = 0;
			hasGrown = false;
			growthPercent = 0;

			UpdateSeeds();
			UpdateMoney();
			UpdatePlantedSeeds();
			UpdatePercent();

			if (money > 20000 * sellPrice && !hasWon)
			{
				hasWon = true;
				InfoBox.Text = $"CONGRATULATIONS! YOU WON!";
				System.Diagnostics.Process.Start("https://youtu.be/j5C6X9vOEkU");
			}
		}

		private void Planter()
		{
			if (seeds > 0)
			{
				PlantSeeds();
				return;
			}
			else
			{
				BuySeeds(Clamp(Convert.ToInt32(Math.Floor((double)(money / buyPrice))), 1, fieldSpace));
				return;
			}
		}

		private void BuySeeds(int count)
		{
			int cost = count * buyPrice;

			if (money < cost)
			{
				InfoBox.Text = $"You need ${cost} in order to buy {count} {IsPlural(count)}";
				return;
			}

			if (seeds + count > 50000000)
			{
				InfoBox.Text = $"Bruh, you don't need THAT many seeds!";
				return;
			}

			seeds += count;
			UpdateSeeds();

			money -= cost;
			UpdateMoney();

			InfoBox.Text = $"You successfully bought {count} {IsPlural(count)} for ${cost}";
		}

		private void Store(Items Item)
		{
			if (money < ItemStore[Item])
			{
				InfoBox.Text = $"You need ${ItemStore[Item]} in order to buy a {Item}";
				return;
			}

			if (BoughtItems.ContainsKey(Item))
			{
				InfoBox.Text = $"You already own {Item}";
				return;
			}

			money -= ItemStore[Item];
			BoughtItems.Add(Item, true);
			UpdateMoney();

			InfoBox.Text = $"You successfully bought {Item}";

			UpdateEquipment();
		}

		private void ExpandField()
		{
			Items item = Items.Expansion;

			if (money < ItemStore[item])
			{
				InfoBox.Text = $"You need ${ItemStore[item]} in order to buy a {item}";
				return;
			}
			else if(fieldSpace > 499960)
			{
				InfoBox.Text = $"Your field is already at maximum size";
				return;
			}

			fieldSpace += 50;
			money -= ItemStore[item];

			UpdateMoney();
			UpdateFieldSpace();
		}

		private string IsPlural(int count)
		{
			if (count == 1)
				return "seed";
			else return "seeds";
		}

		private int Clamp(int value, int Minimum, int Maximum)
		{
			if (value < Minimum)
				return Minimum;
			else if (value > Maximum)
				return Maximum;
			else return value;
		}


		private void CreateButtons()
		{
			BuyExpansion.Content = $"Buy {Items.Expansion} (${ItemStore[Items.Expansion]})";
			BuyScarecrow.Content = $"Buy {Items.Scarecrow} (${ItemStore[Items.Scarecrow]})";
			BuyHarvester.Content = $"Buy {Items.Harvester} (${ItemStore[Items.Harvester]})";
			BuyPlanter.Content = $"Buy {Items.Planter} (${ItemStore[Items.Planter]})";
			BuySprinkler.Content = $"Buy {Items.Sprinkler} (${ItemStore[Items.Sprinkler]})";
		}


		private void PlantSeed_Click(object sender, RoutedEventArgs e) => PlantSeeds();
		private void Harvest_Click(object sender, RoutedEventArgs e) => HarvestSeeds();
		private void BuySeeds_Click(object sender, RoutedEventArgs e) => BuySeeds((int)SeedQuantity.Value);

		private void SeedQuantity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			int value = Clamp((int)e.NewValue, 1, 20);

			if (BuySeedsButton == null)
				return;

			BuySeedsButton.Content = $"Buy {value} {IsPlural(value)} (${value * buyPrice})";
		}

		private void BuyScarecrow_Click(object sender, RoutedEventArgs e) => Store(Items.Scarecrow);
		private void BuyHarvester_Click(object sender, RoutedEventArgs e) => Store(Items.Harvester);
		private void BuySprinkler_Click(object sender, RoutedEventArgs e) => Store(Items.Sprinkler);
		private void BuyPlanter_Click(object sender, RoutedEventArgs e) => Store(Items.Planter);
		private void ExpandField_Click(object sender, RoutedEventArgs e) => ExpandField();

		private void ChangeMarket_Click(object sender, RoutedEventArgs e)
		{
			if(money < 20000)
            {
				InfoBox.Text = $"You need $20000 to change the market";
				return;
			}
			if (buyPrice < 4)
			{
				InfoBox.Text = $"You have already changed the market you sell at";
				return;
			}

			buyPrice = 3;
			sellPrice = 11;

			money -= 20000;

			InfoBox.Text = $"You have successfully change the market you buy and sell seeds at";

			int value = (int)SeedQuantity.Value;
			BuySeedsButton.Content = $"Buy {value} {IsPlural(value)} (${value * buyPrice})";
		}



		private void UpdateSeeds() => SeedBox.Content = seeds < 1 ? "No seeds" : $"{seeds} {IsPlural(seeds)}";
		private void UpdatePlantedSeeds() => PlantedSeeds.Content = plantedSeeds < 1 ? "No seeds planted" : $"{plantedSeeds} {IsPlural(plantedSeeds)}";
		private void UpdateMoney() => MoneyBox.Content = $"${money}";
		private void UpdatePercent() => GrowthBox.Content = $"{growthPercent}%";
		private void UpdateFieldSpace() => FieldSpace.Content = $"{fieldSpace}";
		private void UpdateEquipment() => EquipmentBox.Content = string.Join(", ", BoughtItems.Keys);

		private void WaterField_Click(object sender, RoutedEventArgs e)
		{
			if ((DateTime.Now - LastActivity).TotalSeconds < 1)
			{
				InfoBox.Text = "Please wait before watering your field again";
				return;
			}
			else if(plantedSeeds < 1)
			{
				InfoBox.Text = "There are no crops in your field to water";
				return;
			}
			else if (hasGrown)
			{
				InfoBox.Text = "Your seeds are already fully grown";
				return;
			}

			LastActivity = DateTime.Now;

			growthPercent++;
			UpdatePercent();

			if(growthPercent == 100)
				hasGrown = true;
			

			InfoBox.Text = "You have watered your field, increasing the crop growth by 1%";
		}
	}
}

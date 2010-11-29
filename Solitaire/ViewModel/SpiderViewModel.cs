﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spider.Engine;
using Spider.GamePlay;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace Spider.Solitaire.ViewModel
{
    public class SpiderViewModel : ViewModelBase
    {
        private List<int> checkPoints;

        public SpiderViewModel()
        {
            Variation = Variation.Spider2;
            AlgorithmType = AlgorithmType.Study;

            checkPoints = new List<int>();

            NewCommand = new RelayCommand(param => New());
            ExitCommand = new RelayCommand(param => Exit());
            UndoCommand = new RelayCommand(param => Undo(), param => CanUndo());
            DealCommand = new RelayCommand(param => Deal(), param => CanDeal());
            MoveCommand = new RelayCommand(param => Move(), param => CanMove());
            SelectCommand = new RelayCommand(param => Select((CardViewModel)param), param => CanSelect((CardViewModel)param));

            DiscardPiles = new PileViewModel();
            Piles = new ObservableCollection<PileViewModel>();
            StockPile = new PileViewModel();
            MovePile = new PileViewModel();

            if (IsInDesignMode)
            {
                string data = @"
                @2||KhTh3s5h9s-Ah-5hKsAs7sKs-Jh-7sKs8s8h-9hJh--6s3hQh-7s9s8h
                Jh-9s3hJh4s|7h6h5h4h3h2hAh-2s-7h6s5s4s-KhQsJsTs9s-2sAs-Th-Js
                Ts-2h-Kh-2s|9hTsAs9h3sQsJs5sTh4s8s3sQh9h8h2s5hQsAhTh3s4s5s2h
                8sAh7h6h6s4h4h8hQh5sQsTsAs7sKh2h6hKs8s4hQhJs6s3h6h7h@
            ";
                Game = new Game(data, AlgorithmType);
            }
            else
            {
                Game = new Game(Variation, AlgorithmType);
            }
            Refresh();
        }

        public ICommand NewCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        public ICommand UndoCommand { get; private set; }
        public ICommand DealCommand { get; private set; }
        public ICommand MoveCommand { get; private set; }
        public ICommand SelectCommand { get; private set; }

        public Game Game { get; private set; }
        public PileViewModel DiscardPiles { get; private set; }
        public ObservableCollection<PileViewModel> Piles { get; private set; }
        public PileViewModel StockPile { get; private set; }
        public PileViewModel MovePile { get; private set; }
        public CardViewModel FromCard { get; private set; }
        public CardViewModel ToCard { get; private set; }

        public Tableau Tableau { get { return Game.Tableau; } }

        private Variation Variation { get; set; }
        private AlgorithmType AlgorithmType { get; set; }

        /// <summary>
        /// Raised when this workspace should be removed from the UI.
        /// </summary>
        public event EventHandler RequestClose;

        void OnRequestClose()
        {
            EventHandler handler = this.RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void New()
        {
            Game = new Game(Variation, AlgorithmType);
            checkPoints.Clear();
            Game.Start();
            Refresh();
        }

        private void Exit()
        {
            OnRequestClose();
        }

        private void Undo()
        {
            Tableau.Revert(checkPoints[checkPoints.Count - 1]);
            checkPoints.RemoveAt(checkPoints.Count - 1);
            Refresh();
        }

        private bool CanUndo()
        {
            return checkPoints.Count > 0;
        }

        private void Deal()
        {
            checkPoints.Add(Tableau.CheckPoint);
            Tableau.Deal();
            Refresh();
        }

        private bool CanDeal()
        {
            return Tableau.StockPile.Count > 0;
        }

        private void Move()
        {
            checkPoints.Add(Tableau.CheckPoint);
            if (!Game.MakeMove() && Tableau.StockPile.Count > 0)
            {
                Tableau.Deal();
            }
            Refresh();
        }

        private bool CanMove()
        {
            return true;
        }

        private void Select(CardViewModel card)
        {
            if (card.Column == -1 && card.Row == -1)
            {
                checkPoints.Add(Tableau.CheckPoint);
                Deal();
                ResetMoveAndRefresh();
                return;
            }

            if (FromCard == null)
            {
                FromCard = card;
                Refresh();
                return;
            }

            ToCard = card;
            if (FromCard.Column == ToCard.Column)
            {
                ResetMoveAndRefresh();
                return;
            }

            Move move = new Move(FromCard.Column, FromCard.Row, ToCard.Column);
            if (Tableau.MoveIsValid(move))
            {
                checkPoints.Add(Tableau.CheckPoint);
                Tableau.Move(move);
                ResetMoveAndRefresh();
                return;
            }

            ResetMoveAndRefresh();
        }

        private void ResetMoveAndRefresh()
        {
            FromCard = null;
            ToCard = null;
            Refresh();
        }

        private bool CanSelect(CardViewModel card)
        {
            return card != null && card.IsSelectable;
        }

        private void Refresh()
        {
            Refresh(DiscardPiles, GetDiscardCards());

            while (Piles.Count > Game.NumberOfPiles)
            {
                Piles.RemoveAt(Piles.Count - 1);
            }
            while (Piles.Count < Game.NumberOfPiles)
            {
                Piles.Add(new PileViewModel());
            }
            for (int column = 0; column < Game.NumberOfPiles; column++)
            {
                Refresh(Piles[column], GetPileCards(column));
            }

            Refresh(StockPile, GetStockCards());
            Refresh(MovePile, GetMoveCards());
        }

        private void Refresh(ObservableCollection<CardViewModel> collection, IEnumerable<CardViewModel> cards)
        {
            int i = 0;
            foreach (var card in cards)
            {
                if (i == collection.Count)
                {
                    collection.Add(card);
                }
                else if (collection[i].GetType() != card.GetType() || !collection[i].Equals(card))
                {
                    collection[i] = card;
                }
                ++i;
            }
            while (i < collection.Count)
            {
                collection.RemoveAt(collection.Count - 1);
            }
        }

        private IEnumerable<CardViewModel> GetDiscardCards()
        {
            for (int i = 0; i < Tableau.DiscardPiles.Count; i++)
            {
                Pile pile = Tableau.DiscardPiles[i];
                yield return new UpCardViewModel { Card = pile[pile.Count - 1] };
            }
        }

        private IEnumerable<CardViewModel> GetPileCards(int column)
        {
            if (FromCard != null && FromCard.Column == column)
            {
                for (int row = 0; row < Tableau.DownPiles[column].Count; row++)
                {
                    Card card = Tableau.DownPiles[column][row];
                    bool isSelectable = FromCard.Row == 0 && row == Tableau.DownPiles[column].Count - 1;
                    yield return new DownCardViewModel { Card = card, Column = column, IsSelectable = isSelectable };
                }
                for (int row = 0; row < FromCard.Row; row++)
                {
                    Card card = Tableau.UpPiles[column][row];
                    yield return new UpCardViewModel { Card = card, Column = column, Row = row, IsSelectable = true };
                }
            }
            else
            {
                for (int row = 0; row < Tableau.DownPiles[column].Count; row++)
                {
                    Card card = Tableau.DownPiles[column][row];
                    yield return new DownCardViewModel { Card = card };
                }
                for (int row = 0; row < Tableau.UpPiles[column].Count; row++)
                {
                    Card card = Tableau.UpPiles[column][row];
                    yield return new UpCardViewModel { Card = card, Column = column, Row = row, IsSelectable = true };
                }
                if (Tableau.IsSpace(column))
                {
                    yield return new EmptySpaceViewModel { Column = column, IsSelectable = true };
                }
            }
        }

        private IEnumerable<CardViewModel> GetStockCards()
        {
            Pile stockPile = Tableau.StockPile;
            for (int i = 0; i < stockPile.Count; i += Game.NumberOfPiles)
            {
                bool isSelectable = i + Game.NumberOfPiles >= stockPile.Count;
                yield return new DownCardViewModel { Card = Card.Empty, Column = -1, Row = -1, IsSelectable = isSelectable };
            }
        }

        private IEnumerable<CardViewModel> GetMoveCards()
        {
            if (FromCard != null)
            {
                Pile fromPile = Tableau.UpPiles[FromCard.Column];
                for (int row = FromCard.Row; row < fromPile.Count; row++)
                {
                    yield return new UpCardViewModel { Card = fromPile[row] };
                }
            }
        }
    }
}
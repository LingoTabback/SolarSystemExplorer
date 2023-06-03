using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ephemeris
{

	public static class VSOPSeries
	{
		public static readonly double[][,] MercuryL = {
			VSOPTerms.MercuryL0,
			VSOPTerms.MercuryL1,
			VSOPTerms.MercuryL2,
			VSOPTerms.MercuryL3,
			VSOPTerms.MercuryL4,
			VSOPTerms.MercuryL5
		};


		public static readonly double[][,] MercuryB = {
			VSOPTerms.MercuryB0,
			VSOPTerms.MercuryB1,
			VSOPTerms.MercuryB2,
			VSOPTerms.MercuryB3,
			VSOPTerms.MercuryB4,
			VSOPTerms.MercuryB5
		};

		public static readonly double[][,] MercuryR = {
			VSOPTerms.MercuryR0,
			VSOPTerms.MercuryR1,
			VSOPTerms.MercuryR2,
			VSOPTerms.MercuryR3,
			VSOPTerms.MercuryR4
		};


		public static readonly double[][,] VenusL = {
			VSOPTerms.VenusL0,
			VSOPTerms.VenusL1,
			VSOPTerms.VenusL2,
			VSOPTerms.VenusL3,
			VSOPTerms.VenusL4,
			VSOPTerms.VenusL5
		};

		public static readonly double[][,] VenusB = {
			VSOPTerms.VenusB0,
			VSOPTerms.VenusB1,
			VSOPTerms.VenusB2,
			VSOPTerms.VenusB3,
			VSOPTerms.VenusB4,
			VSOPTerms.VenusB5
		};

		public static readonly double[][,] VenusR = {
			VSOPTerms.VenusR0,
			VSOPTerms.VenusR1,
			VSOPTerms.VenusR2,
			VSOPTerms.VenusR3,
			VSOPTerms.VenusR4
		};


		public static readonly double[][,] EarthL = {
			VSOPTerms.EarthL0,
			VSOPTerms.EarthL1,
			VSOPTerms.EarthL2,
			VSOPTerms.EarthL3,
			VSOPTerms.EarthL4,
			VSOPTerms.EarthL5
		};

		public static readonly double[][,] EarthB = {
			VSOPTerms.EarthB0,
			VSOPTerms.EarthB1,
			VSOPTerms.EarthB2
		};

		public static readonly double[][,] EarthR = {
			VSOPTerms.EarthR0,
			VSOPTerms.EarthR1,
			VSOPTerms.EarthR2,
			VSOPTerms.EarthR3,
			VSOPTerms.EarthR4,
			VSOPTerms.EarthR5
		};


		public static readonly double[][,] MarsL = {
			VSOPTerms.MarsL0,
			VSOPTerms.MarsL1,
			VSOPTerms.MarsL2,
			VSOPTerms.MarsL3,
			VSOPTerms.MarsL4,
			VSOPTerms.MarsL5
		};

		public static readonly double[][,] MarsB = {
			VSOPTerms.MarsB0,
			VSOPTerms.MarsB1,
			VSOPTerms.MarsB2,
			VSOPTerms.MarsB3,
			VSOPTerms.MarsB4,
			VSOPTerms.MarsB5
		};

		public static readonly double[][,] MarsR = {
			VSOPTerms.MarsR0,
			VSOPTerms.MarsR1,
			VSOPTerms.MarsR2,
			VSOPTerms.MarsR3,
			VSOPTerms.MarsR4,
			VSOPTerms.MarsR5
		};


		public static readonly double[][,] JupiterL = {
			VSOPTerms.JupiterL0,
			VSOPTerms.JupiterL1,
			VSOPTerms.JupiterL2,
			VSOPTerms.JupiterL3,
			VSOPTerms.JupiterL4,
			VSOPTerms.JupiterL5
		};

		public static readonly double[][,] JupiterB = {
			VSOPTerms.JupiterB0,
			VSOPTerms.JupiterB1,
			VSOPTerms.JupiterB2,
			VSOPTerms.JupiterB3,
			VSOPTerms.JupiterB4,
			VSOPTerms.JupiterB5
		};

		public static readonly double[][,] JupiterR = {
			VSOPTerms.JupiterR0,
			VSOPTerms.JupiterR1,
			VSOPTerms.JupiterR2,
			VSOPTerms.JupiterR3,
			VSOPTerms.JupiterR4,
			VSOPTerms.JupiterR5
		};


		public static readonly double[][,] SaturnL = {
			VSOPTerms.SaturnL0,
			VSOPTerms.SaturnL1,
			VSOPTerms.SaturnL2,
			VSOPTerms.SaturnL3,
			VSOPTerms.SaturnL4,
			VSOPTerms.SaturnL5
		};

		public static readonly double[][,] SaturnB = {
			VSOPTerms.SaturnB0,
			VSOPTerms.SaturnB1,
			VSOPTerms.SaturnB2,
			VSOPTerms.SaturnB3,
			VSOPTerms.SaturnB4,
			VSOPTerms.SaturnB5
		};

		public static readonly double[][,] SaturnR = {
			VSOPTerms.SaturnR0,
			VSOPTerms.SaturnR1,
			VSOPTerms.SaturnR2,
			VSOPTerms.SaturnR3,
			VSOPTerms.SaturnR4,
			VSOPTerms.SaturnR5
		};


		public static readonly double[][,] UranusL = {
			VSOPTerms.UranusL0,
			VSOPTerms.UranusL1,
			VSOPTerms.UranusL2,
			VSOPTerms.UranusL3,
			VSOPTerms.UranusL4
		};

		public static readonly double[][,] UranusB = {
			VSOPTerms.UranusB0,
			VSOPTerms.UranusB1,
			VSOPTerms.UranusB2,
			VSOPTerms.UranusB3
		};

		public static readonly double[][,] UranusR = {
			VSOPTerms.UranusR0,
			VSOPTerms.UranusR1,
			VSOPTerms.UranusR2,
			VSOPTerms.UranusR3,
			VSOPTerms.UranusR4
		};


		public static readonly double[][,] NeptuneL = {
			VSOPTerms.NeptuneL0,
			VSOPTerms.NeptuneL1,
			VSOPTerms.NeptuneL2,
			VSOPTerms.NeptuneL3
		};

		public static readonly double[][,] NeptuneB = {
			VSOPTerms.NeptuneB0,
			VSOPTerms.NeptuneB1,
			VSOPTerms.NeptuneB2,
			VSOPTerms.NeptuneB3
		};

		public static readonly double[][,] NeptuneR = {
			VSOPTerms.NeptuneR0,
			VSOPTerms.NeptuneR1,
			VSOPTerms.NeptuneR2,
			VSOPTerms.NeptuneR3,
			VSOPTerms.NeptuneR4
		};

	}
}
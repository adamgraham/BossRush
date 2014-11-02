﻿using UnityEngine;
using System.Collections;

public class SpiderTankTurboState : SpiderTankState
{
	public float turretSpeed;
	public float canonDelay;
	public int amountPerWave;

	public float duration;

	[Range( 0.0f, 1.0f )]
	public float laserChance;

	public NavigateTowardsTarget movementScript;

	public override void Awake()
	{
		base.Awake();
		movementScript = GetComponent<NavigateTowardsTarget>();
		movementScript.target = player;
	}

	void OnEnable()
	{
		spiderTank.mainCanon.SetCooldown( canonDelay );
		spawner.baseAmountPerWave = amountPerWave;
		spawner.enabled = true;
		movementScript.enabled = true;

		// register for health trigger callbacks
		spiderTank.RegisterHealthTriggerCallback( HealthTriggerCallback );

		Invoke( "TransitionOut", duration );
	}

	void Update()
	{
		spiderTank.LookAllGuns( turretSpeed );
		spiderTank.FireAllGuns();
	}

	void OnDisable()
	{
		spawner.enabled = false;
		movementScript.enabled = false;
		spiderTank.DeregisterHealthTriggerCallback( HealthTriggerCallback );
	}

	void TransitionOut()
	{
		enabled = false;

		if ( Random.value < laserChance )
		{
			spiderTank.laserSpin.enabled = true;
		}
		else
		{
			spiderTank.basicState.enabled = true;
		}
	}
}

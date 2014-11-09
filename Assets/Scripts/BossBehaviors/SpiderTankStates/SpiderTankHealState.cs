﻿using UnityEngine;
using System.Collections;

public class SpiderTankHealState : SpiderTankState
{
	public int mininionCount;

	public float healRate;

	public override void Awake()
	{
		base.Awake();
	}

	void OnEnable()
	{
		spawner.Spawn( mininionCount );
		spawner.RegisterEnemyCountCallback( MinionCountChange );
		shield.SetActive( true );
		Physics.IgnoreCollision( collider, shield.collider, true );
	}

	public void Update()
	{
		spiderTank.health.Heal( healRate * Time.deltaTime );
		if ( spiderTank.health.atMaxHealth )
		{
			MinionCountChange( 0 ); // kinda janky, but whatevs
		}
	}

	public void OnDisable()
	{
		spawner.DeregisterEnemyCountCallback( MinionCountChange );
		shield.SetActive( false );
	}

	public void MinionCountChange( int count )
	{
		if ( this != null && enabled && count == 0 )
		{
			enabled = false;
			spawner.enabled = false;

			spiderTank.SetDamageBase();

			spiderTank.basicState.enabled = true;
		}
	}
}
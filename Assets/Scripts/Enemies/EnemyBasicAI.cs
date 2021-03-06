﻿using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Animator))]
public class EnemyBasicAI : MonoBehaviour
{
	public enum EnemyType
	{
		Slime,
		Archer,
		Mage
	}

	[Header("Properties:")]
	public int health;
	public int damage;
	public float movementSpeed;
	public float attackRate, attackRange;
	public float projectileSpeed;

	[Space]
	public EnemyType enemyType;

#pragma warning disable 0649

	[Header("Components:")]

	[SerializeField]
	protected GameObject projectilePrefab;

	[SerializeField]
	protected GameObject deathParticleEffect;

	[SerializeField]
	protected Transform ejectionPort;

#pragma warning restore 0649

	public GameObject target;

	protected Animator animatorComponent;
	protected Rigidbody2D enemyRigidbody;
	protected SpriteRenderer enemySpriteRenderer;

	protected float attackTimer;
	protected bool isDeathAnimationPlaying = false;

	private void Start()
	{
		// Get all of the components
		animatorComponent = GetComponent<Animator>();
		enemyRigidbody = GetComponent<Rigidbody2D>();
		enemySpriteRenderer = GetComponent<SpriteRenderer>();
	}

	protected void Update()
	{
		attackTimer += Time.deltaTime;

		// Flip enemy to the player position
		if (enemyType != EnemyType.Slime)
		{
			FlipSprite();
		}

		if (target != null)
		{
			Act();
		}
		else
		{
			if (Game.game.playerShootingBehaviour)
			{
				// Set the player as the target
				target = Game.game.playerGameObject;
			}
			else
			{
				// Freeze enemies, if the player doesn't exist
				enemyRigidbody.simulated = false;
			}
		}
	}

	private void Act()
	{
		// Chase the player if he's too far away
		if (Vector3.Distance(transform.position, target.transform.position) > attackRange && !isDeathAnimationPlaying)
		{
			ChaseTarget();

			if (GameUtilities.CheckAnimatorParameter(animatorComponent, "IsRunning"))
			{
				animatorComponent.SetBool("IsRunning", true);
			}
		}

		// Otherwise attack
		else
		{
			if (GameUtilities.CheckAnimatorParameter(animatorComponent, "IsRunning"))
			{
				animatorComponent.SetBool("IsRunning", false);
			}

			if (attackTimer >= attackRate)
			{
				attackTimer = 0.0f;
				Attack();
			}
		}
	}

	#region TakeDamage

	public void TakeDamage(int damage)
	{
		if (health - damage <= 0)
		{
			Game.game.cameraShakeController.StartShake(.5f, .07f);
			Die();
		}
		else
		{
			health -= damage;
			StartCoroutine(DamageFlash());
		}
	}

	private IEnumerator DamageFlash()
	{
		enemySpriteRenderer.color = Color.red;
		yield return new WaitForSeconds(0.03f);
		enemySpriteRenderer.color = Color.white;
	}

	public virtual void Die()
	{
		isDeathAnimationPlaying = true;
		enemyRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;

		if (GameUtilities.CheckAnimatorParameter(animatorComponent, "IsGoingToDie"))
		{
			animatorComponent.SetBool("IsGoingToDie", true);
		}
	}

	#endregion

	#region Attack

	protected virtual void ChaseTarget()
	{
		// Move towards the target
		transform.position = Vector3.MoveTowards(transform.position, target.transform.position, movementSpeed * Time.deltaTime);
	}

	protected virtual void Attack()
	{
		if (isDeathAnimationPlaying)
		{
			return;
		}

		/*
			If ranged enemy - shoot a projectile.
			Otherwise - hit the target with the melee attack.
		*/

		switch (enemyType)
		{
			case EnemyType.Archer:
			case EnemyType.Mage:
				Shoot();
				break;
			case EnemyType.Slime:
				Melee();
				break;
		}
	}

	protected virtual void Shoot()
	{
		GameObject proj = Instantiate(projectilePrefab, ejectionPort.position, transform.rotation);
		Projectile projScript = proj.GetComponent<Projectile>();

		// Set projectile's damage and shoot it forward
		projScript.damage = damage;

		switch (enemyType)
		{
			case EnemyType.Mage:
				projScript.followSpeed = projectileSpeed;
				break;
			default:
				// Set projectile rotation
				Vector3 dir = transform.position - target.transform.position;
				float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90;
				projScript.projectileRigidbody.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

				projScript.projectileRigidbody.velocity = (target.transform.position - transform.position).normalized * projectileSpeed;
				
				break;
		}
	}

	protected virtual void Melee()
	{
		// Damage the player, with a small knockback
		SoundController.soundController.Play(GameConstants.PLAYER_HIT_SFX);
		Game.game.playerHealthController.TakeDamage(damage);
		enemyRigidbody.AddForce((target.transform.position - transform.position).normalized * -3 * Time.deltaTime);
	}

	#endregion

	protected void DestroyChildrens()
	{
		foreach (Transform child in transform)
		{
			Destroy(child.gameObject);
		}
	}

	protected void DestroyGameObject()
	{
		Game.game.curEnemies.Remove(gameObject);
		Destroy(gameObject);
	}

	private void FlipSprite()
	{
		// Flip sprite to the player position
		bool playerIsToTheRight = Vector2.Dot(
			Game.game.playerGameObject.transform.position - transform.position,
			transform.right) > 0;
		enemySpriteRenderer.flipX = !playerIsToTheRight;
	}
}

